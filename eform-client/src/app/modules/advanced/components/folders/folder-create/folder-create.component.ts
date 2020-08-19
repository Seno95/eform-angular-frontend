import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { FoldersService } from 'src/app/common/services/advanced/folders.service';
import { FolderCreateModel } from 'src/app/common/models/advanced/folder-create.model';
import { FolderDto } from 'src/app/common/models/dto/folder.dto';

@Component({
  selector: 'app-folder-create',
  templateUrl: './folder-create.component.html',
})
export class FolderCreateComponent implements OnInit {
  @Output() folderCreated: EventEmitter<void> = new EventEmitter<void>();
  @ViewChild('frame', { static: true }) frame;
  newFolderModel: FolderCreateModel = new FolderCreateModel();
  selectedParentFolder: FolderDto;

  constructor(private foldersService: FoldersService) {}

  ngOnInit(): void {}

  show(selectedFolder?: FolderDto) {
    if (selectedFolder) {
      this.selectedParentFolder = selectedFolder;
    }
    this.frame.show();
  }

  hide() {
    this.selectedParentFolder = null;
  }

  createFolder() {
    if (this.selectedParentFolder) {
      this.newFolderModel = {...this.newFolderModel, parentId: this.selectedParentFolder.id};
    }
    this.foldersService.createFolder(this.newFolderModel).subscribe((data) => {
      if (data && data.success) {
        this.selectedParentFolder = null;
        this.newFolderModel = new FolderCreateModel();
        this.folderCreated.emit();
        this.frame.hide();
      }
    });
  }
}
