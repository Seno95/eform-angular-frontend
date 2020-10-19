import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationMenuPageComponent } from './components';
import { NavigationMenuRouting } from './navigation-menu.routing';
import {SharedPnModule} from 'src/app/plugins/modules/shared/shared-pn.module';
import {ButtonsModule, CardsModule, CollapseModule, InputsModule, TableModule, TooltipModule} from 'angular-bootstrap-md';
import {TranslateModule} from '@ngx-translate/core';
import {FontAwesomeModule} from '@fortawesome/angular-fontawesome';
import {NgSelectModule} from '@ng-select/ng-select';
import {DragulaModule} from 'ng2-dragula';
import {FormsModule} from '@angular/forms';
import { NavigationMenuItemComponent } from './components/navigation-menu-item/navigation-menu-item.component';
import { NavigationMenuTemplateItemComponent } from './components/navigation-menu-template-item/navigation-menu-template-item.component';

@NgModule({
  declarations: [
    NavigationMenuPageComponent,
    NavigationMenuItemComponent,
    NavigationMenuTemplateItemComponent,
  ],
  imports: [
    CommonModule,
    NavigationMenuRouting,
    SharedPnModule,
    ButtonsModule,
    TranslateModule,
    FontAwesomeModule,
    NgSelectModule,
    InputsModule,
    TableModule,
    CardsModule,
    DragulaModule,
    TooltipModule,
    FormsModule,
    CollapseModule,
  ],
})
export class NavigationMenuModule {}
