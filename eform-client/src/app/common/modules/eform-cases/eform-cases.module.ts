import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import {
  OWL_DATE_TIME_FORMATS,
  OwlDateTimeModule,
  OwlNativeDateTimeModule,
} from '@danielmoncada/angular-datetime-picker';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { GalleryModule } from '@ngx-gallery/core';
import { GallerizeModule } from '@ngx-gallery/gallerize';
import { LightboxModule } from '@ngx-gallery/lightbox';
import { TranslateModule } from '@ngx-translate/core';
import { MDBBootstrapModule } from 'angular-bootstrap-md';
import { MY_MOMENT_FORMATS } from 'src/app/common/helpers';
import { EformImportedModule } from 'src/app/common/modules/eform-imported/eform-imported.module';
import { EformSharedModule } from 'src/app/common/modules/eform-shared/eform-shared.module';
import {
  CaseEditConfirmationComponent,
  CaseEditElementComponent,
  CaseEditNavComponent,
  CaseEditSwitchComponent,
  CasePostNewComponent,
  CasePostsPageComponent,
  CasePostViewComponent,
  CaseRemoveModalComponent,
  ElementAudioComponent,
  ElementCheckboxComponent,
  ElementCommentComponent,
  ElementContainerComponent,
  ElementDateComponent,
  ElementEntitysearchComponent,
  ElementEntityselectComponent,
  ElementInfoboxComponent,
  ElementMultiselectComponent,
  ElementNumberComponent,
  ElementNumberStepperComponent,
  ElementPdfComponent,
  ElementPictureComponent,
  ElementSignatureComponent,
  ElementSingleselectComponent,
  ElementTextComponent,
  ElementTimerComponent,
  CaseArchiveModalComponent,
  DeletePictureDialogComponent,
  AddPictureDialogComponent,
} from './components';

import { casePostsPersistProvider } from './components/case-posts/store';
import {MatSortModule} from '@angular/material/sort';
import {MatCheckboxModule} from '@angular/material/checkbox';
import {MatButtonModule} from '@angular/material/button';
import {MatIconModule} from '@angular/material/icon';
import {MatCardModule} from '@angular/material/card';
import {MatRadioModule} from '@angular/material/radio';
import {MatTooltipModule} from '@angular/material/tooltip';
import {MatDialogModule} from '@angular/material/dialog';
import {MatInputModule} from '@angular/material/input';
import {MtxSelectModule} from '@ng-matero/extensions/select';
import {NgSelectModule} from '@ng-select/ng-select';

@NgModule({
    imports: [
        TranslateModule,
        MDBBootstrapModule,
        EformSharedModule,
        CommonModule,
        EformImportedModule,
        GallerizeModule,
        LightboxModule,
        GalleryModule,
        OwlDateTimeModule,
        OwlNativeDateTimeModule,
        FormsModule,
        FontAwesomeModule,
        RouterModule,
        MatSortModule,
        MatCheckboxModule,
        MatButtonModule,
        MatIconModule,
        MatCardModule,
        MatRadioModule,
        MatTooltipModule,
        MatDialogModule,
        MatInputModule,
        MtxSelectModule,
        NgSelectModule,
    ],
  declarations: [
    CaseEditNavComponent,
    CaseEditSwitchComponent,
    CaseEditElementComponent,
    CaseEditConfirmationComponent,
    ElementTextComponent,
    ElementNumberComponent,
    ElementNumberStepperComponent,
    ElementCheckboxComponent,
    ElementSingleselectComponent,
    ElementPdfComponent,
    ElementAudioComponent,
    ElementDateComponent,
    ElementCommentComponent,
    ElementEntityselectComponent,
    ElementEntitysearchComponent,
    ElementMultiselectComponent,
    ElementInfoboxComponent,
    ElementTimerComponent,
    CaseRemoveModalComponent,
    ElementContainerComponent,
    ElementPictureComponent,
    ElementSignatureComponent,
    CasePostNewComponent,
    CasePostViewComponent,
    CasePostsPageComponent,
    CaseArchiveModalComponent,
    DeletePictureDialogComponent,
    AddPictureDialogComponent,
  ],
  providers: [
    { provide: OWL_DATE_TIME_FORMATS, useValue: MY_MOMENT_FORMATS },
    casePostsPersistProvider,
  ],
  exports: [
    CaseEditNavComponent,
    CaseEditSwitchComponent,
    CaseEditElementComponent,
    CaseEditConfirmationComponent,
    ElementTextComponent,
    ElementNumberComponent,
    ElementNumberStepperComponent,
    ElementCheckboxComponent,
    ElementSingleselectComponent,
    ElementPdfComponent,
    ElementAudioComponent,
    ElementDateComponent,
    ElementCommentComponent,
    ElementEntityselectComponent,
    ElementEntitysearchComponent,
    ElementMultiselectComponent,
    ElementInfoboxComponent,
    ElementTimerComponent,
    CaseRemoveModalComponent,
    ElementContainerComponent,
    ElementPictureComponent,
    ElementSignatureComponent,
    CasePostNewComponent,
    CasePostViewComponent,
    CasePostsPageComponent,
    CaseArchiveModalComponent,
    DeletePictureDialogComponent,
    AddPictureDialogComponent,
  ],
})
export class EformCasesModule {}
