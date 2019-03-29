import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import {FormsModule} from '@angular/forms';
import {TranslateModule} from '@ngx-translate/core';
import {NgSelectModule} from '@ng-select/ng-select';
import {EformSharedModule} from '../../common/modules/eform-shared/eform-shared.module';
import {MDBBootstrapModule} from '../../../../port/angular-bootstrap-md';

import {PluginsManagementRouting} from './plugins-management.routing';
import { InstalledPluginEditComponent, InstalledPluginsPageComponent,
  MarketplacePluginsPageComponent, MarketplacePluginInstallComponent } from './components';
import {FontAwesomeModule} from '@fortawesome/angular-fontawesome';


@NgModule({
  imports: [
    CommonModule,
    EformSharedModule,
    MDBBootstrapModule,
    FormsModule,
    TranslateModule,
    NgSelectModule,
    PluginsManagementRouting,
    FontAwesomeModule
  ],
  declarations: [InstalledPluginsPageComponent, InstalledPluginEditComponent,
    MarketplacePluginsPageComponent, MarketplacePluginInstallComponent]
})
export class PluginsManagementModule { }
