import { Component, OnInit, AfterViewInit } from '@angular/core';
import { UUID } from 'angular2-uuid';
import { FileItem, FileUploader, FileUploaderOptions } from 'ng2-file-upload';
import { EventBrokerService } from 'src/app/common/helpers';
import { AdminSettingsModel } from 'src/app/common/models';
import { AppSettingsService } from 'src/app/common/services';
import { AuthStateService } from 'src/app/common/store';
import {AppSettingsQuery, AppSettingsStateService} from 'src/app/modules/application-settings/components/store';
import * as R from 'ramda';

@Component({
  selector: 'app-admin-settings',
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.scss'],
})
export class AdminSettingsComponent implements OnInit, AfterViewInit {
  loginPageImageUploader: FileUploader = new FileUploader({
    url: '/api/images/login-page-images',
  });
  headerImageUploader: FileUploader = new FileUploader({
    url: '/api/images/eform-images',
  });
  headerImageLink: string;
  loginPageImageLink: string;
  spinnerStatus: boolean;
  latestVersion: string;
  adminSettingsModel: AdminSettingsModel = new AdminSettingsModel();
  othersSettings: { isEnableWidget: boolean } = {isEnableWidget: false};
  previousAdminSettings: AdminSettingsModel;

  constructor(
    private settingsService: AppSettingsService,
    private authStateService: AuthStateService,
    private eventBrokerService: EventBrokerService,
    private appSettingsStateService: AppSettingsStateService,
    private appSettingsQuery: AppSettingsQuery,
  ) {}

  get currentRole(): string {
    return this.authStateService.currentRole;
  }

  ngAfterViewInit() {}

  ngOnInit() {
    if (this.currentRole === 'admin') {
      this.getSettings();
    }

    const uploadOptions: FileUploaderOptions = {
      headers: [
        {
          name: 'Authorization',
          value: this.authStateService.bearerToken,
        },
      ],
    };
    this.loginPageImageUploader.setOptions(uploadOptions);
    this.headerImageUploader.setOptions(uploadOptions);
  }

  getLatestVersion() {
    this.settingsService.getLatestVersion().subscribe((operation) => {
      if (operation && operation.success) {
        this.latestVersion = operation.model;
      }
    });
  }

  initializeUploaders() {
    this.loginPageImageUploader.onAfterAddingFile = (_) => {
      if (this.loginPageImageUploader.queue.length > 1) {
        // save only last file
        this.loginPageImageUploader.removeFromQueue(this.loginPageImageUploader.queue[0]);
      }
    };
    this.headerImageUploader.onAfterAddingFile = (_) => {
      if (this.headerImageUploader.queue.length > 1) {
        // save only last file
        this.headerImageUploader.removeFromQueue(this.headerImageUploader.queue[0]);
      }
    };
    this.loginPageImageUploader.onAfterAddingAll = (files: FileItem[]) => {
      files.forEach((fileItem) => {
        fileItem.file.name = `${UUID.UUID()}.${R.last(fileItem.file.name.split('.'))}`; // uuid + file extension
        this.adminSettingsModel.loginPageSettingsModel.imageLink = fileItem.file.name;
      });
    };
    this.headerImageUploader.onAfterAddingAll = (files: FileItem[]) => {
      files.forEach((fileItem) => {
        fileItem.file.name = `${UUID.UUID()}.${R.last(fileItem.file.name.split('.'))}`; // uuid + file extension
        this.adminSettingsModel.headerSettingsModel.imageLink = fileItem.file.name;
      });
    };
  }

  getSettings() {
    //this.appSettingsStateService.getAdminSettings();
    this.appSettingsStateService.getAllAppSettings();
    this.appSettingsQuery.selectAllSettings$.subscribe((allSettings) => {
      const adminSettings = allSettings.adminSettings;
      const othersSettings = allSettings.othersSettings;
      this.initializeUploaders();
      this.getLatestVersion();
      if (adminSettings) {
        this.adminSettingsModel = adminSettings;
        this.previousAdminSettings = adminSettings;

        if (this.adminSettingsModel.headerSettingsModel.imageLink) {
          this.headerImageLink =
            'api/images/eform-images?fileName=' +
            this.adminSettingsModel.headerSettingsModel.imageLink;
        } else {
          this.headerImageLink = '../../../assets/images/logo.png';
        }

        if (this.adminSettingsModel.loginPageSettingsModel.imageLink) {
          this.loginPageImageLink =
            'api/images/login-page-images?fileName=' +
            this.adminSettingsModel.loginPageSettingsModel.imageLink;
        } else {
          this.loginPageImageLink = '../../../assets/images/eform-phone.jpg';
        }
      }
      if (othersSettings) {
        this.othersSettings = {...this.othersSettings, isEnableWidget: othersSettings.isUserbackWidgetEnabled};
      }
    });
  }

  updateAdminSettings() {
    if (this.headerImageUploader.queue.length > 0) {
      this.headerImageUploader.queue[0].upload();
    }
    if (this.loginPageImageUploader.queue.length > 0) {
      this.loginPageImageUploader.queue[0].upload();
    }

    //if (!R.equals(this.adminSettingsModel, this.previousAdminSettings)) { // TODO: fix this, it doesn't work
      this.appSettingsStateService
        .updateAdminSettings(this.adminSettingsModel)
        .subscribe((operation) => {
          if (operation && operation.success) {
            this.headerImageUploader.clearQueue();
            this.loginPageImageUploader.clearQueue();
            this.eventBrokerService.emit<void>('get-header-settings', null);
          }
        });
    //}

    this.updateOtherSettings();
  }

  updateOtherSettings() {
    this.appSettingsStateService.updateUserbackWidgetIsEnabled(this.othersSettings.isEnableWidget)
      .subscribe((operation) => {
        if (operation && operation.success) {
        //
        }
      });
  }

  resetLoginPageSettings() {
    this.settingsService.resetLoginPageSettings().subscribe((operation) => {
      if (operation && operation.success) {
        this.appSettingsStateService.getAllAppSettings();
      }
    });
  }

  resetHeaderSettings() {
    this.settingsService.resetHeaderSettings().subscribe((operation) => {
      if (operation && operation.success) {
        this.appSettingsStateService.getAllAppSettings();
      }
    });
  }
}
