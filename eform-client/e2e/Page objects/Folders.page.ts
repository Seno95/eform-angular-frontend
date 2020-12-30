import {PageWithNavbarPage} from './PageWithNavbar.page';
import myEformsPage from './MyEforms.page';
import {Guid} from 'guid-typescript';
import {expect} from 'chai';
import {DeviceUsersRowObject} from './DeviceUsers.page';

class FoldersPage extends PageWithNavbarPage {
  constructor() {
    super();
  }

  public get newFolderBtn() {
    return $('#newFolderBtn');
  }

  public get createNameInput() {
    return $('#name');
  }

  public get createDescriptionInput() {
    return $('#description');
  }

  public get saveCreateBtn() {
    return $('#folderSaveBtn');
  }

  public get cancelCreateBtn() {
    return $('#cancelCreateBtn');
  }

  public get saveDeleteBtn() {
    const saveDeleteBtn = $('#saveDeleteBtn');
    saveDeleteBtn.waitForDisplayed({timeout: 20000});
    saveDeleteBtn.waitForClickable({timeout: 20000});
    return saveDeleteBtn;
  }

  public get cancelDeleteBtn() {
    const ele = $('#cancelDeleteBtn');
    ele.waitForDisplayed({timeout: 20000});
    ele.waitForClickable({timeout: 20000});
    return ele;
  }

  public get editNameInput() {
    return $('#editNameInput');
  }

  public get editDescriptionInput() {
    return $('#editDescriptionInput');
  }

  public get saveEditBtn() {
    return $('#saveEditBtn');
  }

  public get cancelEditBtn() {
    return $('#cancelEditBtn');
  }

  public get rowNum(): number {
    if (!$('#folderTreeId').isExisting()) {
      browser.pause(500);
    }
    return $$('#folderTreeId').length;
  }

  public get rowNumParents(): number {
    browser.pause(500);
    return $$('#folderTreeName').length;
  }

  getFolder(num): FoldersRowObject {
    return new FoldersRowObject(num);
  }

  getFolderByName(nameFolder: string): FoldersRowObject {
    for (let i = 1; i < this.rowNum + 1; i++) {
      const folder = new FoldersRowObject(i);
      if (folder.name === nameFolder) {
        return folder;
      }
    }
    return null;
  }

  getFolderFromTree(num): FoldersTreeRowObject {
    return new FoldersTreeRowObject(num);
  }

  public createNewFolder(name: string, description: string) {
    this.newFolderBtn.click();
    $('#name').waitForDisplayed({timeout: 10000});
    this.createNameInput.setValue(name);
    this.createDescriptionInput.setValue(description);
    this.saveCreateBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 90000, reverse: true});
    $('#newFolderBtn').waitForDisplayed({timeout: 20000});
  }

  public editFolder(folder: FoldersRowObject, name = '', description = '') {
    if (!folder.editBtn.isExisting()) {
      folder.folderElement.click();
      folder.editBtn.waitForDisplayed({timeout: 20000});
    }
    folder.editBtn.click();
    this.editNameInput.waitForDisplayed({timeout: 20000});
    if (name != null) {
      // this.editNameInput.click();
      this.editNameInput.clearValue();
      this.editNameInput.setValue(name);
    }
    if (description != null) {
      // this.editDescriptionInput.click();
      this.editDescriptionInput.clearValue();
      this.editDescriptionInput.setValue(description);
    }
    this.saveEditBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 20000, reverse: true});
    this.newFolderBtn.waitForDisplayed({timeout: 20000});
  }

  public createFolderChild(rowNum, name = '', description = '') {
    $$('#folderTreeName')[rowNum - 1].click();
    $$('#createFolderChildBtn')[0].waitForDisplayed({timeout: 10000});
    $$('#createFolderChildBtn')[0].click();
    $('#name').waitForDisplayed({timeout: 10000});
    this.createNameInput.setValue(name);
    this.createDescriptionInput.setValue(description);
    this.saveCreateBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 90000, reverse: true});
  }

  public deleteFolderChild(parentRowNum: number, childRowToDelete: number) {
    $$('#folderTreeOpenClose')[parentRowNum - 1].waitForDisplayed({timeout: 10000});
    $$('#folderTreeOpenClose')[parentRowNum - 1].click();
    browser.pause(1000);
    $$('#folderTreeName')[childRowToDelete - 1].waitForDisplayed({timeout: 10000});
    $$('#folderTreeName')[childRowToDelete - 1].click();
    $('#deleteFolderTreeBtn').click();
    this.saveDeleteBtn.click();
  }

  public editFolderChild(folder: FoldersTreeRowObject, name = '', description = '') {
    folder.editTreeBtn.click();
    if (name != null) {
      this.editNameInput.waitForDisplayed({timeout: 20000});
      this.editNameInput.click();
      this.editNameInput.clearValue();
      this.editNameInput.setValue(name);
    }
    if (description != null) {
      this.editDescriptionInput.waitForDisplayed({timeout: 20000});
      this.editDescriptionInput.click();
      this.editDescriptionInput.clearValue();
      this.editDescriptionInput.setValue(description);
    }
    this.saveEditBtn.click();
    $('#newFolderBtn').waitForDisplayed({timeout: 20000});
  }

  public editFolderTree(folder: FoldersTreeRowObject, name = '', description = '') {
    if (!folder.editTreeBtn.isExisting()) {
      folder.folderTreeElement.click();
      folder.editTreeBtn.waitForDisplayed({timeout: 20000});
    }
    folder.editTreeBtn.click();
    $('#editNameInput').waitForDisplayed({timeout: 20000});
    if (name != null) {
      this.editNameInput.click();
      this.editNameInput.clearValue();
      this.editNameInput.setValue(name);
    }
    if (description != null) {
      this.editDescriptionInput.click();
      this.editDescriptionInput.clearValue();
      this.editDescriptionInput.setValue(description);
    }
    this.saveEditBtn.click();
    $('#newFolderBtn').waitForDisplayed({timeout: 20000});
  }
}

const foldersPage = new FoldersPage();
export default foldersPage;

export class FoldersRowObject {
  constructor(rowNum) {
    if ($$('#folderTreeId')[rowNum - 1]) {
      try {
      this.folderElement = $$('#folderTreeId')[rowNum - 1];
      } catch (e) {
      }
      try {
        this.name = this.folderElement.$('#folderTreeName').getText();
      } catch (e) {
      }
      // try {
      //   this.description = this.folderElement.$('#folderTreeDescription').getText();
      // } catch (e) {
      // }
      this.editBtn = this.folderElement.$('#editFolderTreeBtn');
      this.deleteBtn = this.folderElement.$('#deleteFolderTreeBtn');
      this.createFolderChildBtn = this.folderElement.$('#createFolderChildBtn');
    }
  }

  folderElement;
  name;
  // description;
  editBtn;
  deleteBtn;
  createFolderChildBtn;

  getDescription(): string {
    if (!this.editBtn.isExisting()) {
      this.folderElement.click();
      this.editBtn.waitForDisplayed({timeout: 20000});
    }
    this.editBtn.click();
    foldersPage.cancelEditBtn.waitForClickable({timeout: 20000});
    const description =  foldersPage.editDescriptionInput.getValue();
    foldersPage.cancelEditBtn.click();
    return description;
  }

  createChild (name = '', description = '') {
    if (!this.createFolderChildBtn.isExisting()) {
    this.folderElement.click();
    this.createFolderChildBtn.waitForDisplayed({timeout: 20000});
  }
    this.createFolderChildBtn.click();
    $('#name').waitForDisplayed({timeout: 10000});
    foldersPage.createNameInput.setValue(name);
    foldersPage.createDescriptionInput.setValue(description);
    foldersPage.saveCreateBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 90000, reverse: true});
  }

  delete () {
    if (!this.deleteBtn.isExisting()) {
      this.folderElement.click();
      this.deleteBtn.waitForDisplayed({timeout: 20000});
    }
    // browser.pause(500);
    this.deleteBtn.click();
    foldersPage.saveDeleteBtn.waitForClickable({ timeout: 20000});
    foldersPage.saveDeleteBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 2000, reverse: true});
  }
  editFolder(name = '', description = '') {
    if (!this.editBtn.isExisting()) {
      this.folderElement.click();
      this.editBtn.waitForDisplayed({timeout: 20000});
    }
    this.editBtn.click();
    foldersPage.editNameInput.waitForDisplayed({timeout: 20000});
    if (name != null) {
      // this.editNameInput.click();
      foldersPage.editNameInput.clearValue();
      foldersPage.editNameInput.setValue(name);
    }
    if (description != null) {
      // this.editDescriptionInput.click();
      foldersPage.editDescriptionInput.clearValue();
      foldersPage.editDescriptionInput.setValue(description);
    }
    foldersPage.saveEditBtn.click();
    $('#spinner-animation').waitForDisplayed({timeout: 20000, reverse: true});
    foldersPage.newFolderBtn.waitForDisplayed({timeout: 20000});
  }
}

export class FoldersTreeRowObject {
  constructor(rowNum) {
    if ($$('#folderTreeId')[rowNum - 1]) {
      try {
        this.folderTreeElement = $$('#folderTreeId')[rowNum - 1];
      } catch (e) {
      }
      try {
        this.nameTree = $$('#folderTreeName')[rowNum - 1].getText();
      } catch (e) {
      }
      // try {
      //   this.descriptionTree = $$('#folderTreeDescription')[rowNum - 1].getText();
      // } catch (e) {
      // }
      this.editTreeBtn = $('#editFolderTreeBtn');
      this.deleteTreeBtn = $('#deleteFolderTreeBtn');
    }
  }

  folderTreeElement;
  nameTree;
  // descriptionTree;
  editTreeBtn;
  deleteTreeBtn;

  getDescription(): string {
    if (!this.editTreeBtn.isExisting()) {
      this.folderTreeElement.click();
      this.editTreeBtn.waitForDisplayed({timeout: 20000});
    }
    this.editTreeBtn.click();
    foldersPage.cancelEditBtn.waitForClickable({timeout: 20000});
    const description =  foldersPage.editDescriptionInput.getValue();
    foldersPage.cancelEditBtn.click();
    return description;
  }
}
