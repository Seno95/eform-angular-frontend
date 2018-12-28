import {NgModule} from '@angular/core';
import {RouterModule, Routes} from '@angular/router';
import {EformsPageComponent} from './components/index';

const routes: Routes = [
  {
    path: '',
    component: EformsPageComponent,
  }
  // {
  //   path: 'report/:eformId',
  //   loadChildren: './eform-report/eform-report.module#EformReportModule',
  //   data: {
  //     eformId: 1
  //   }
  // },
];


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EformsRouting {
}
