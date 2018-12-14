import {Component, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {Subject} from 'rxjs';
import {debounceTime} from 'rxjs/operators';
import {ApplicationPages} from 'src/app/common/enums';
import {CommonDictionaryModel} from 'src/app/common/models/common';
import {TemplateDto} from 'src/app/common/models/dto';
import {SavedTagModel, TemplateListModel, TemplateRequestModel} from 'src/app/common/models/eforms';
import {PageSettingsModel} from 'src/app/common/models/settings';
import {AuthService, UserSettingsService} from 'src/app/common/services/auth';
import {EFormService, EFormTagService} from 'src/app/common/services/eform';

@Component({
  selector: 'app-eform-page',
  templateUrl: './eforms-page.component.html',
  styleUrls: ['./eforms-page.component.scss']
})
export class EformsPageComponent implements OnInit, OnDestroy {

  @ViewChild('modalNewEform') newEformModal;
  @ViewChild('modalCasesColumns') modalCasesColumnsModal;
  @ViewChild('modalParing') modalPairing;
  @ViewChild('modalEditTags') modalEditTags;
  @ViewChild('modalRemoveEform') modalRemoveEform;
  @ViewChild('modalUploadZip') modalUploadZip;

  searchSubject = new Subject();

  spinnerStatus = false;
  localPageSettings: PageSettingsModel = new PageSettingsModel();
  templateRequestModel: TemplateRequestModel = new TemplateRequestModel;
  templateListModel: TemplateListModel = new TemplateListModel();
  availableTags: Array<CommonDictionaryModel> = [];

  items = [
    'New',
    'Legacy',
    'Test1'
  ];

  constructor(private eFormService: EFormService,
              private authService: AuthService,
              private userSettingsService: UserSettingsService,
              private eFormTagService: EFormTagService
  ) {
    this.searchSubject.pipe(
      debounceTime(500)
    ). subscribe(val => {
      this.templateRequestModel.nameFilter = val.toString();
      this.loadAllTemplates();
    });
  }

  ngOnInit() {
    this.getLocalPageSettings();
  }

  ngOnDestroy() {
    this.searchSubject.unsubscribe();
  }

  getLocalPageSettings() {
    this.localPageSettings = this.userSettingsService.getLocalPageSettings
    ('pagesSettings', ApplicationPages[ApplicationPages.Eforms])
      .settings;
    this.loadAllTags();
  }

  updateLocalPageSettings(localStorageItemName: string) {
    this.userSettingsService.updateLocalPageSettings
    (localStorageItemName, this.localPageSettings, ApplicationPages[ApplicationPages.Eforms]);
    this.getLocalPageSettings();
  }

  loadAllTemplates() {
    this.spinnerStatus = true;
    this.templateRequestModel.sort = this.localPageSettings.sort;
    this.templateRequestModel.isSortDsc = this.localPageSettings.isSortDsc;
    this.eFormService.getAll(this.templateRequestModel).subscribe(operation => {
      this.spinnerStatus = false;
      if (operation && operation.success) {
        this.templateListModel = operation.model;
      }
    });
  }

  loadAllTags() {
      this.spinnerStatus = true;
      this.eFormTagService.getAvailableTags().subscribe((data) => {
        if (data && data.success) {
          this.availableTags = data.model;
          this.loadSelectedUserTags();
        }
      }, (error) => {
        this.spinnerStatus = false;
      });
  }

  saveTag(e: any) {
    const savedTagModel = new SavedTagModel();
    savedTagModel.tagId = e.id;
    savedTagModel.tagName = e.name;
    this.spinnerStatus = true;
    this.eFormTagService.addSavedTag(savedTagModel).subscribe((data) => {
      if (data && data.success) {
        this.templateRequestModel.tagIds.push(e.id);
        this.loadAllTemplates();
      }
    }, (error) => {
      this.spinnerStatus = false;
    });
  }

  removeSavedTag(e: any) {
    this.spinnerStatus = true;
    this.eFormTagService.deleteSavedTag(e.value.id).subscribe(data => {
      if (data && data.success) {
        this.templateRequestModel.tagIds = this.templateRequestModel.tagIds.filter(x => x !== e.id);
        this.loadAllTemplates();
      }
    }, (error) => {
      this.spinnerStatus = false;
    });
  }

  loadSelectedUserTags() {
    this.spinnerStatus = true;
    this.eFormTagService.getSavedTags().subscribe((data) => {
      if (data && data.success) {
        this.templateRequestModel.tagIds = data.model.tagList.map(x => x.tagId);
        this.loadAllTemplates();
      }
    }, (error) => {
      this.spinnerStatus = false;
    });
  }

  onLabelInputChanged(label: string) {
    this.searchSubject.next(label);
  }

  sortTable(sort: string) {
    if (this.localPageSettings.sort === sort) {
      this.localPageSettings.isSortDsc = !this.localPageSettings.isSortDsc;
    } else {
      this.localPageSettings.isSortDsc = false;
      this.localPageSettings.sort = sort;
    }
    this.updateLocalPageSettings('pagesSettings');
  }

  openNewEformModal() {
    this.newEformModal.show();
  }

  openEditColumnsModal(templateDto: TemplateDto) {
    this.modalCasesColumnsModal.show(templateDto);
  }

  openEformDeleteModal(templateDto: TemplateDto) {
    this.modalRemoveEform.show(templateDto);
  }

  uploadZipFile(templateDto: TemplateDto) {
    this.modalUploadZip.show(templateDto);
  }

  downloadItem(itemName: string, templateId: number) {
    if (itemName == 'XML') {
      window.open('/api/template-files/download-eform-xml/' + templateId, '_blank');
    } else {
      window.open('/api/template-files/csv/' + templateId, '_blank');
    }
  }

  openPairingModal(templateDto: TemplateDto) {
    this.modalPairing.show(templateDto);
  }

  openEditTagsModal(templateDto: TemplateDto) {
    this.modalEditTags.show(templateDto);
  }
}
