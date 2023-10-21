// This is just a flag to check if current document is same as clicked document. If title matched then donot load file again
var currentDocumentId = '';

var currentDocumentEmlContent = '';

var currentFolderName = '';

var currentFolderNameWithExtension = '';

var currentFolderUrl = '';

var relatedDocumentContent = '';

var localFolderList = '';
var folderListInLocalStorage = '';
var loadAzureFolder = false;

var clickedRemoveFolder = false;
var filteredOutFolders = [];
var filteredFolders = [];
var currentFolderTitleList = {};
var totalPages = 0;
var currentPage = 0;
var maxPage = 0;
var isLicenseExpired = false;
var baseUrl = window.location.href;
var currentDocumentCount = 0;

// on page load this method will call to fetch folders from localstorage if there is any.
setLicenseSessionExpiryDate();
fetchFolderFromLocalStorage();
loadDefaultContent();
window.history.forward();

showHideDateSearch(true);
var searchTextId = document.getElementById("searchTextId");
var searchTextIdWithDate = document.getElementById("searchTextIdWithDate");
var relatedDocumentContentId = document.getElementById("relatedDocumentList");
var searchInput = document.getElementById("searchInput");
var searchWithSuggestionInput = document.getElementById("searchWithSuggestionInput");
var folderSuggestions = document.getElementById("folderSuggestions");
var resultBox = document.getElementById("resultBox");
var searchInProgressId = document.getElementById("searchInProgressId");
var searchInProgressIdDate = document.getElementById("searchInProgressIdDate");

function showHideDateSearch(flag) {
    if (flag) {
        var searchDivWithoutDateId = document.getElementById("searchDivWithoutDate");
        searchDivWithoutDateId.style.display = "block";
        var searchDivWithDateId = document.getElementById("searchDivWithDate");
        searchDivWithDateId.style.display = "none";
    }
    else {
        var searchDivWithoutDateId = document.getElementById("searchDivWithoutDate");
        searchDivWithoutDateId.style.display = "none";
        var searchDivWithDateId = document.getElementById("searchDivWithDate");
        searchDivWithDateId.style.display = "block";
    }
}

// This method is to load folders from localstorage. First we are storing folders to local storage then pulling all folders from localstorage
function fetchFolderFromLocalStorage() {
    licenseExpiryDialog(isLicenseExpired);
    showSpinner();
    var folderListLocalStorage = localStorage.getItem("folderList");
    var folderListDivContent = document.getElementById("folderList");
    var folderListContent = "";
    var currentSessionFolder = sessionStorage.getItem("currentSessionFolder");
    var folderList = JSON.parse(folderListLocalStorage);
    if (folderList != null && folderList.length != 0) {
        for (var i = 0; i < folderList.length; i++) {
            folderListContent += " <li onclick='loadFolderTitle(\"" + folderList[i].FolderName + "\"," + true + ",\"" + folderList[i].FolderId + "\");' id=" + encodeURIComponent(folderList[i].FolderName) + " class='loadFolder'> <i class='fa fa-folder folderIcon'></i>" + folderList[i].FolderName + " <i class='fa fa-trash deleteIcon' title='Remove' onclick='removeFolder(\"" + folderList[i].FolderId + ",,-" + folderList[i].FolderName + "\");'></i> </li>";
        }
        folderListDivContent.innerHTML = folderListContent;
        if (currentSessionFolder == '' || currentSessionFolder == null) {
            loadFolderTitle(folderList[0].FolderName);
        }
        else {
            loadFolderTitle(currentSessionFolder);
        }
    }
    else {
        folderListDivContent.innerHTML = '';
        if (!clickedRemoveFolder)
            loadFolderTitle();
    }
    hideSpinner();
}

function loadDefaultContent() {
    var titleListLocalStorage = localStorage.getItem("titleList");
    var titleListObject = JSON.parse(titleListLocalStorage);
    var showPageLoadContent = document.getElementById("showPageLoadContentId");
    if (titleListObject == null || titleListObject?.length <= 0) {
        if (showPageLoadContent) {
            showPageLoadContent.style.display = "block";
        }
    }
    else {
        if (showPageLoadContent) {
            showPageLoadContent.style.display = "none";
        }
    }
    showArrowUp();
}

function arrowUpClick() {
    showArrowDown();
    sortTitle("titleList", false);
}

function arrowDownClick() {
    showArrowUp();
    sortTitle("titleList", true);
}

function showArrowUp() {
    document.getElementById("arrowUpId").style.display = "block";
    document.getElementById("arrowDownId").style.display = "none";
}

function showArrowDown() {
    document.getElementById("arrowUpId").style.display = "none";
    document.getElementById("arrowDownId").style.display = "block";
}

// This method is to fetch titles from localstorage based on folder name. 
function loadFolderTitle(folderName, checkPermission, folderId) {
    licenseExpiryDialog(isLicenseExpired);
    if (clickedRemoveFolder) {
        clickedRemoveFolder = false;
        currentFolderName = '';
        return false;
    }
    //if (folderName != currentFolderName) {
    if (checkPermission == undefined || checkPermission == false || !checkIfLicenseSessionExpired()) {
        loadTitle(folderName);
        changeFolderBackgroundColor();
    }
    else {
        showSpinner();
        checkFolderPermission(folderId).then(function (isValid) {
            if (isValid == true) {
                loadTitle(folderName);
                changeFolderBackgroundColor();
            }
            else {
                removeFolder(folderId);
            }
            hideSpinner();
        });
    }
    //}
}

function changeFolderBackgroundColor() {
    var folderStyling = document.getElementsByClassName("loadFolder");
    if (folderStyling) {
        for (var i = 0; i < folderStyling.length; i++) {
            folderStyling[i].style.backgroundColor = 'white';
        }
    }
    var selectedFolderId = document.getElementById(encodeURIComponent(currentFolderName));
    if (selectedFolderId) {
        selectedFolderId.style.backgroundColor = "#f5f3ed";
    }
}

function loadTitle(folderName) {
    licenseExpiryDialog(isLicenseExpired);
    currentFolderName = folderName;
    sessionStorage.setItem("currentSessionFolder", currentFolderName);
    showSpinner();
    var titleListLocalStorage = localStorage.getItem("titleList");
    var titleListObject = JSON.parse(titleListLocalStorage)
    for (let key in titleListObject) {
        if (titleListObject[key].FolderName == folderName) {
            readTitleFromJson(titleListObject[key].FolderPath, folderName);
            break;
        }
    }
}

function loadFileListContent(titleListObject) {
    var titleListDivContent = document.getElementById("titleList");
    var listContent = "";
    var docCount = document.getElementById("documentCount");
    var titleList = JSON.parse(titleListObject.FileData);
    currentFolderNameWithExtension = titleListObject.FolderPath.split('\\').pop();
    currentFolderUrl = titleListObject.FolderPath;
    for (var i = 0; i < titleList.length; i++) {
        listContent += "<li data-toggle='tooltip' data-placement='auto' onclick='loadFile(\"" + titleList[i].Docid + "\");' id=" + titleList[i].Docid + " title=\"" + titleList[i].XA_Title + "\">" + titleList[i].XA_Title + "</li>";
    }
    docCount.innerHTML = titleList.length + ' docs';
    currentDocumentCount = titleList.length;
    var emlResponseDiv = document.getElementById("emlResponse");
    emlResponseDiv.innerHTML = "";
    titleListDivContent.innerHTML = listContent;
}

function readTitleFromJson(path, folderName) {
    licenseExpiryDialog(isLicenseExpired);
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/ReadTitleJsonFile?folderPath=' + path,
        success: function (response) {
            currentFolderTitleList = { FolderName: folderName, FileData: response, FolderPath: path }
            loadFileListContent(currentFolderTitleList);
            hideSpinner();
            closeFolderDialog();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}


function checkFolderPermission(folderId) {
    licenseExpiryDialog(isLicenseExpired);
    return new Promise(function (resolve, reject) {
        if (folderId != undefined) {
            $.ajax({
                type: 'Get',
                async: true,
                contentType: "application/json;charset=utf-8",
                url: baseUrl + 'home/folderPermission?folderId=' + folderId,
                success: function (msg) {
                    if (msg == false) {
                        removeFolder(folderId);
                        resolve(false);
                    }
                    else {
                        resolve(true);
                    }
                },
                error: function (e, x) {
                    hideSpinner();
                    reject(true)
                }
            });
        }
    });
}

// This method is to send eml(string) data to backend and get html in response, then show this response to UI
function GetEmlDataFromBackend(filePath, fileName) {
    licenseExpiryDialog(isLicenseExpired);
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/GetHtmlFromEmlFilePath?filePath=' + filePath + '&fileName=' + fileName,
        success: function (msg, status, jqXHR) {
            var emlResponseDiv = document.getElementById("emlResponse");
            emlResponseDiv.innerHTML = msg;
            loadLinkedDocument();
            loadRelatedDocument();
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

// This method is to remove folder from localstorage then refresh folder lsit
function removeFolder(folderId) {
    var deleteFolderDetails = [];
    var removeFolderFromLocalstorage = false;
    if (folderId.indexOf(",,-") > -1) {
        deleteFolderDetails = folderId.split(",,-");
        removeFolderFromLocalstorage = true;
    }
    licenseExpiryDialog(isLicenseExpired);
    clickedRemoveFolder = true;
    currentFolderName = '';
    var folderListLocalStorage = localStorage.getItem("folderList");
    var folderLists = JSON.parse(folderListLocalStorage);
    var filteredFolder = folderLists.filter(function (value, index, arr) {
        if (removeFolderFromLocalstorage == true) {
            return value.FolderId != folderId && value.FolderName != deleteFolderDetails[1];
        }
        return value.FolderId != folderId;
    });
    localStorage.setItem("folderList", JSON.stringify(filteredFolder));

    var titleListLocalStorage = localStorage.getItem("titleList");
    var titleLists = JSON.parse(titleListLocalStorage);
    var filteredTitle = titleLists.filter(function (el) {
        if (removeFolderFromLocalstorage == true) {
            return el.FolderId != folderId && el.FolderName != deleteFolderDetails[1];
        }
        return el.FolderId != folderId;
    });
    localStorage.setItem("titleList", JSON.stringify(filteredTitle));
    fetchFolderFromLocalStorage();
    var titleListDivContent = document.getElementById("titleList");
    titleListDivContent.innerHTML = '';
    var emlResponseDiv = document.getElementById("emlResponse");
    emlResponseDiv.innerHTML = "";
}

// This method will trigger when you click on document. Here we have on condition to check if you clicked the same document which is already opened then it will not load data again it will be as it is.
function loadFile(docId) {
    licenseExpiryDialog(isLicenseExpired);
    var currentTitle = document.getElementById(currentDocumentId);
    if (currentDocumentId != '' && currentDocumentId != docId && currentTitle != null) {
        currentTitle.style.backgroundColor = "white";
    }
    if (currentDocumentId !== docId) {
        var relatedDocumentContentId = document.getElementById("relatedDocumentList");
        relatedDocumentContentId.innerHTML = '';
        relatedDocumentContent = '';
        var currentTitle = document.getElementById(docId);
        if (currentTitle != null) {
            currentTitle.style.backgroundColor = "#f5f3ed";
        }
        currentDocumentId = docId;
        GetEmlDataFromBackend(currentFolderUrl, docId);
    }
}

// Find notes filed and notes header property once its loaded then show notes dialog box
function loadNotesDetailsFunc() {
    licenseExpiryDialog(isLicenseExpired);
    var fields = document.getElementById("fields"); //fields meta
    var meta = document.getElementById("meta"); //fields meta
    var fieldProperties = "";
    if (meta)
        fieldProperties = "<p class='documentTitle'>Notes Headers</p>" + meta.innerHTML;
    if (fields)
        fieldProperties += "</hr> <p class='documentTitle'>Notes Fields</p>" + fields.innerHTML;
    document.getElementById("notesDialogBox").style.visibility = "visible";
    var notesHtml = document.getElementById("notesDialogBody");
    notesHtml.innerHTML = fieldProperties;
}

// hide notes field and notes header dialog box once user click on close icon
function closeNotesDialog() {
    var notesHtml = document.getElementById("notesDialogBox");
    notesHtml.style.visibility = "hidden";
}

function closeFolderDialog() {
    var notesHtml = document.getElementById("foldersDialogBox");
    notesHtml.style.visibility = "hidden";
    localFolderList = '';
    resultBox.innerHTML = localFolderList;
}

function loadLinkedDocument() {
    licenseExpiryDialog(isLicenseExpired);
    var relatedDocumentList = [];
    const findRelatedDocumentRegex = new RegExp(`<a href="\/${currentFolderNameWithExtension}\/.*?>.*?<\/a>`, 'g');
    const filter = document.getElementById("emlResponse").innerHTML.match(findRelatedDocumentRegex);
    var titleList = JSON.parse(currentFolderTitleList.FileData);
    for (var i = 0; i < filter?.length; i++) {
        var splitData = filter[i].split('?')[0].split('/');
        var relatedDocumentId = splitData[splitData.length - 1];
        if (relatedDocumentId.toLowerCase() != currentDocumentId.toLowerCase()) {
            for (var i = 0; i < titleList.length; i++) {
                if (titleList[i].Docid.toLowerCase() == relatedDocumentId.toLowerCase()) {
                    relatedDocumentList.push(titleList[i].XA_Title);
                    relatedDocumentContent += "<li data-toggle='tooltip' data-placement='right' title='" + titleList[i].XA_Title + "' onclick='loadFile(\"" + titleList[i].Docid + "\");' id=" + titleList[i].Docid + ">" + titleList[i].XA_Title + "</li>";
                }
            }
        }
    }
    relatedDocumentContentId.innerHTML = relatedDocumentContent;
}

function loadRelatedDocument() {
    licenseExpiryDialog(isLicenseExpired);
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/SearchRelatedDocument?searchTerm=' + currentDocumentId + '&folderPath=' + currentFolderUrl,
        success: function (msg) {
            var fileList = msg.split(',');
            var relatedDocumentList = [];
            var titleList = JSON.parse(currentFolderTitleList.FileData);
            for (var file = 0; file < fileList.length; file++) {
                for (var i = 0; i < titleList.length; i++) {
                    if (titleList[i].Docid.toLowerCase() == fileList[file].toLowerCase()) {
                        relatedDocumentList.push(titleList[i].XA_Title);
                        relatedDocumentContent += "<li data-toggle='tooltip' data-placement='right' title='" + titleList[i].XA_Title + "' onclick='loadFile(\"" + titleList[i].Docid + "\");' id=" + titleList[i].Docid + ">" + titleList[i].XA_Title + "</li>";
                    }
                }
            }
            relatedDocumentContentId.innerHTML = relatedDocumentContent;
        },
        error: function (e, x) {
        }
    });
}

searchTextId.addEventListener("keypress", function (event) {
    if (event.key === "Enter") {
        event.preventDefault();
        searchWord(false);
    }
});

searchTextIdWithDate.addEventListener("keypress", function (event) {
    if (event.key === "Enter") {
        event.preventDefault();
        searchWord(true);
    }
});

function searchWord(withDate) { 
    licenseExpiryDialog(isLicenseExpired);
    var startDate = document.getElementById("startDate").value;
    var endDate = document.getElementById("endDate").value;
    var searchText = '';
    if (withDate) {
        searchText = searchTextIdWithDate.value;
    }
    else {
        searchText = searchTextId.value;
    }
    if (searchText != '' || startDate != null || endDate != null) {
        if (searchText == '' && startDate == '' && endDate == '') {
            return false;
        }
        $.ajax({
            type: 'Get',
            contentType: "application/json;charset=utf-8",
            url: baseUrl + 'home/searchDocuments?searchTerm=' + searchText + '&folderPath=' + currentFolderUrl + '&startDate=' + startDate + '&endDate=' + endDate,
            success: function (msg) {
                var fileList = msg.split(',');
                var titleListDivContent = document.getElementById("titleList");
                var titleList = JSON.parse(currentFolderTitleList.FileData);
                var listContent = '';
                var searchCount = 0;
                for (var file = 0; file < fileList.length; file++) {
                    for (var i = 0; i < titleList.length; i++) {
                        if (titleList[i].Docid.toLowerCase() == fileList[file].toLowerCase()) {
                            searchCount++;
                            listContent += "<li data-toggle='tooltip' data-placement='right' title='" + titleList[i].XA_Title + "' onclick='loadFile(\"" + titleList[i].Docid + "\");' id=" + titleList[i].Docid + ">" + titleList[i].XA_Title + "</li>";
                            break;
                        }
                    }
                }
                titleListDivContent.innerHTML = listContent;
                if (withDate) {
                    searchInProgressIdDate.innerHTML = searchCount + ' of ' + currentDocumentCount + ' Results Found';
                }
                else {
                    searchInProgressId.innerHTML = searchCount + ' of ' + currentDocumentCount + ' Results Found';
                }
                hideSpinner();
            },
            error: function (e, x) {
                hideSpinner();
            }
        });
    }
    else {
        loadTitle(currentFolderName);
    }
}

function clearSearch() {
    licenseExpiryDialog(isLicenseExpired);
    searchTextId.value = '';
    searchTextIdWithDate.value = '';
    document.getElementById("startDate").value = "";
    document.getElementById("endDate").value = "";
    document.getElementById('search-input-text').value = '';
    currentDocumentId = '';
    searchInProgressId.innerHTML = '';
    searchInProgressIdDate.innerHTML = '';
    loadTitle(currentFolderName);
}

var showSpinnerMessage = false;
// This method is to show spinner
function showSpinner() {
    showSpinnerMessage = true;
    document.getElementById("spinner").style.display = "block";
}

// Method to hide spinner
function hideSpinner() {
    showSpinnerMessage = false;
    document.getElementById("spinner").style.display = "none";
    document.getElementById("slowLoadText").style.display = "none";
}

setInterval(() => {
    showSpinnerMessageMethod();
}, 3000);

function showSpinnerMessageMethod() {
    if (showSpinnerMessage) {
        document.getElementById("slowLoadText").style.display = "block";
    }  
}

function loadFolderDetailsByPath(path, folderName, folderId) {
    licenseExpiryDialog(isLicenseExpired);
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/ReadTitleJsonFile?folderPath=' + path,
        success: function (response) {
            saveFilesToLocalStorage(decodeURIComponent(folderName), response, decodeURIComponent(path), folderId);
            saveFolderToLocalStorage(decodeURIComponent(folderName), folderId);
            hideSpinner();
            closeFolderDialog();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function loadAzureFolderDetailsByPath(path, folderName) {
    licenseExpiryDialog(isLicenseExpired);
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/ReadAzureTitleJsonFile?folderPath=' + path,
        success: function (response) {
            saveFilesToLocalStorage(decodeURIComponent(folderName), response, decodeURIComponent(path));
            saveFolderToLocalStorage(decodeURIComponent(folderName));
            hideSpinner();
            closeFolderDialog();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function saveFolderToLocalStorage(folderName, folderId) {
    licenseExpiryDialog(isLicenseExpired);
    var folderList = [];
    var folderListLocalStorage = localStorage.getItem("folderList");
    if (folderListLocalStorage == null || folderListLocalStorage == "") {
        var currentFolderData = { FolderName: folderName, FolderId: folderId }
        folderList.push(currentFolderData);
        localStorage.setItem("folderList", JSON.stringify(folderList));
    }
    else {
        var existingFolder = JSON.parse(folderListLocalStorage);
        if (existingFolder.indexOf(folderName) == -1) {
            var currentFolderData = { FolderName: folderName, FolderId: folderId }
            existingFolder.push(currentFolderData);
            localStorage.setItem("folderList", JSON.stringify(existingFolder));
        }
    }
    fetchFolderFromLocalStorage();
}

function saveFilesToLocalStorage(folderName, fileData, folderPath, folderId) {
    licenseExpiryDialog(isLicenseExpired);
    var titleListLocalStorage = localStorage.getItem("titleList");
    if (titleListLocalStorage == null || titleListLocalStorage == "") {
        var currentFolderData = { FolderName: folderName, FileData: null, FolderPath: folderPath, FolderId: folderId }
        var titleList = [];
        titleList.push(currentFolderData);
        localStorage.setItem("titleList", JSON.stringify(titleList));
    }
    else {
        var titleList = JSON.parse(titleListLocalStorage)
        var hasFolder = false;
        for (let key in titleList) {
            if (titleList[key].FolderName == folderName) {
                hasFolder = true; break;
            }
        }
        if (!hasFolder) {
            var currentFolderData = { FolderName: folderName, FileData: null, FolderPath: folderPath, FolderId: folderId }
            titleList.push(currentFolderData);
            localStorage.setItem("titleList", JSON.stringify(titleList));
        }
    }
}

function setLicenseSessionExpiryDate() {
    var data = document.getElementById("errorMessage");
    console.log(data.innerHTML);
    licenseExpiryDialog(isLicenseExpired);
    const date = new Date();
    const licenseValidityExpiryDate = addMinutes(date, 2);
    sessionStorage.setItem("LicenseExpiry", licenseValidityExpiryDate);
}

function checkIfLicenseSessionExpired() {
    licenseExpiryDialog(isLicenseExpired);
    if (new Date(sessionStorage.getItem("LicenseExpiry")) < new Date()) {
        return true;
    }
    else {
        return false;
    }
}

function addMinutes(date, minutes) {
    licenseExpiryDialog(isLicenseExpired);
    date.setMinutes(date.getMinutes() + minutes);
    return date;
}

let result = [];

function folderSubmit() {
    licenseExpiryDialog(isLicenseExpired);
    var inputs = document.querySelectorAll('.folderCheckbox');
    inputs.forEach(item => { // loop all the checkbox item
        if (item.checked) {  //if the check box is checked   
            if (loadAzureFolder) {
                loadAzureFolderDetailsByPath(item.value, item.id);
            }
            else {
                var folderId = item.getAttribute("folderId");
                loadFolderDetailsByPath(item.value, item.id, folderId);
            }
        }
    });
    localFolderList = '';
    resultBox.innerHTML = localFolderList;
}

function checkFolderCheckbox(clickedList) {
    licenseExpiryDialog(isLicenseExpired);
    if (document.getElementById(clickedList).checked == true) {
        document.getElementById(clickedList).checked = false;
    }
    else {
        document.getElementById(clickedList).checked = true;
    }
}

// This will trigger if selected option is folder
function loadLocalFolder() {
    showSpinner();
    licenseExpiryDialog(isLicenseExpired);
    document.getElementById("foldersDialogBox").style.visibility = "visible";
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/LoadFolders',
        success: function (response) {
            localFolderList = response;
            folderListInLocalStorage = localStorage.getItem("folderList");
            filteredFolders = filterFoldersWithInput('');
            var listData = mapLocalFolders();
            showSuggestions(listData);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

searchWithSuggestionInput.addEventListener("keyup", function (e) {
    licenseExpiryDialog(isLicenseExpired);
    let userInput = e.target.value;
    if (userInput) {
        filteredFolders = filterFoldersWithInput(userInput);
        var listData = mapLocalFolders();
        showSuggestions(listData);
    } else {
        filteredFolders = filterFoldersWithInput('');
        var listData = mapLocalFolders();
        showSuggestions(listData);
    }
});

function showSuggestions(list) {
    if (list == '') {
        list = "No archive folders found";
    }
    var listData = '<ul id="allFolderList">' + list + '</ul > ';
    resultBox.innerHTML = listData;
}

function mapLocalFolders() {
    var totalFolders = filteredFolders.length + filteredOutFolders.length;
    maxPage = 5;
    currentPage = 1;
    totalPages = getTotalPages(totalFolders);
    createVisibleButton();
    return createFolderListData();
}

function createFolderListData() {
    var listData = '';
    var localVariableForFilteredFolder = [];
    filteredFolders.map((filteredFolder) => {
        localVariableForFilteredFolder.push(filteredFolder);
    });
    localVariableForFilteredFolder.sort((p1, p2) => (p1.folderName < p2.folderName) ? 1 : (p1.folderName > p2.folderName) ? -1 : 0)
    var localFilteredOutFolders = filteredOutFolders.sort((p1, p2) => (p1 < p2) ? 1 : (p1 > p2) ? -1 : 0);
    localFilteredOutFolders.map((filteredOutFolder) => {
        var filteredOutObject = {};
        filteredOutObject.folderName = filteredOutFolder;
        filteredOutObject.isFilteredOut = true;
        localVariableForFilteredFolder.push(filteredOutObject);
    });

    localVariableForFilteredFolder = paginate(localVariableForFilteredFolder, 10, currentPage);
    localVariableForFilteredFolder.map((localFolder) => {
        if (localFolder.isFilteredOut) {
            listData += "<li style='margin:3px 0px pointer-events:none; opacity:0.6;' title='This folder is already in your archive favourites'><input type='checkbox' class='folderCheckbox' value='" + localFolder.folderName + "' />  " + localFolder.folderName + "</li>";
        }
        else {
            listData += "<li onclick='checkFolderCheckbox(\"" + localFolder.folderName + "\");' style='margin:3px 0px'><input type='checkbox' onclick='checkFolderCheckbox(\"" + localFolder.folderName + "\");' class='folderCheckbox' folderId=" + localFolder.folderId + " value='" + localFolder.folderPath + "' id='" + localFolder.folderName + "' />  " + localFolder.folderName + "</li>";
        }
    });
    return listData;
}

function paginate(array, page_size, page_number) {
    // human-readable page numbers usually start with 1, so we reduce 1 in the first argument
    return array.slice((page_number - 1) * page_size, page_number * page_size);
}

function filterFoldersWithInput(userInput) {
    filteredOutFolders = [];
    var folders = JSON.parse(localFolderList);
    var folderLists = JSON.parse(folderListInLocalStorage);
    filteredFolders = folders.filter((data) => {
        if (data.folderName.toLocaleLowerCase().indexOf(userInput.toLocaleLowerCase()) > -1) {
            var flag = true;
            for (var i = 0; i < folderLists?.length; i++) {
                if (folderLists[i].FolderName.toLocaleLowerCase() == data.folderName.toLocaleLowerCase()) {
                    flag = false;
                    filteredOutFolders.push(folderLists[i].FolderName);
                    break;
                } else {
                    flag = true;
                }
            }
            if (flag) {
                return data.folderName;
            }
        }
    });
    return filteredFolders;
}

function loadAzureFolders() {
    document.getElementById("foldersDialogBox").style.visibility = "visible";
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: baseUrl + 'home/LoadAzureFolders',
        success: function (response) {
            localFolderList = response;
            folderListInLocalStorage = localStorage.getItem("folderList");
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}


function pageNumbers(total, max, current) {
    const half = Math.floor(max / 2);
    let to = max;

    if (current + half >= total) {
        to = total;
    } else if (current > half) {
        to = current + half;
    }

    let from = Math.max(to - max, 0);

    return Array.from({ length: Math.min(total, max) }, (_, i) => (i + 1) + from);
}

function getTotalPages(totalFolderCount) {
    let pages = Math.floor(totalFolderCount / 10);
    let extraFolders = totalFolderCount % 10;
    if (extraFolders) {
        pages++;
    }
    return pages;
}

function createVisibleButton() {
    var paginationIdDiv = document.getElementById("paginationId");
    let pages = pageNumbers(totalPages, maxPage, currentPage);
    var buttonHtml = '';
    if (totalPages > 5 && pages[0] > 1) {
        buttonHtml += '<button onclick="backwardClick()"><i class="fa fa-backward"></i></button>';
    }
    else {
        buttonHtml += '<button class="disableButton"><i class="fa fa-backward"></i></button>';
    }
    if (currentPage != 1) {
        buttonHtml += '<button onclick="leftClick()"><i class="fa fa-chevron-left"></i></button>';
    }
    else {
        buttonHtml += '<button class="disableButton"><i class="fa fa-chevron-left"></i></button>';
    }
    pages.map((pageNumber) => {
        if (currentPage == pageNumber) {
            buttonHtml += '<button class="activePage" id="' + pageNumber + '">' + pageNumber + '</button>';
        }
        else {
            buttonHtml += '<button class="paginationButton" onclick="clickCurrentPage(' + pageNumber + ')">' + pageNumber + '</button>';
        }
    });
    if (totalPages != currentPage) {
        buttonHtml += '<button onclick="rightClick()"><i class="fa fa-chevron-right"></i></button>';
    }
    else {
        buttonHtml += '<button class="disableButton"><i class="fa fa-chevron-right"></i></button>';
    }
    if (totalPages > 5 && pages[0] <= totalPages - 5) {
        buttonHtml += '<button onclick="forwardClick()"><i class="fa fa-forward"></i></button>';
    }
    else {
        buttonHtml += '<button class="disableButton"><i class="fa fa-forward"></i></button>';
    }
    paginationIdDiv.innerHTML = buttonHtml;
}

function searchTitleList(filterBy, inputId) {
    let input, filter, ul, li, txtValue;
    input = document.getElementById(inputId);
    filter = input.value.toUpperCase();
    ul = document.getElementById(filterBy);
    li = ul.getElementsByTagName("li");
    var iHli = ul.getElementsByTagName("li").textContent;
    var ITextli = ul.getElementsByTagName("li").innerText;
    for (let i = 0; i < li.length; i++) {
        txtValue = li[i].textContent || li[i].innerText;
        if (txtValue.toUpperCase().indexOf(filter) > -1) {
            li[i].style.display = "";
        } else {
            li[i].style.display = "none";
        }
    }
}

function sortTitle(filterBy, sortByAsc = true) {
    // Get the unordered list element
    var ul = document.getElementById(filterBy);

    // Get the list items and convert it to an array
    var items = ul.getElementsByTagName('li');
    var itemsArr = Array.from(items);

    // Sort the array of list items alphanumerically
    itemsArr.sort(function (a, b) {
        var textA = (a.textContent || '').trim(); // Use an empty string if text content is null or undefined
        var textB = (b.textContent || '').trim(); // Use an empty string if text content is null or undefined

        // Use regular expressions to separate letters and numbers
        var numA = parseInt((textA.match(/\d+/) || [])[0], 10) || 0; // Extract the first number, or use 0 if no numbers found
        var numB = parseInt((textB.match(/\d+/) || [])[0], 10) || 0; // Extract the first number, or use 0 if no numbers found
        var strA = textA.replace(/\d+/, '');
        var strB = textB.replace(/\d+/, '');

        // Compare strings first
        if (strA !== strB && sortByAsc) {
            return strA.localeCompare(strB);
        }
        else {
            return strB.localeCompare(strA);
        }

        // If strings are the same, compare numbers
        if (sortByAsc)
            return numA - numB;
        else 
            return numB - numA;
    });

    // Remove existing list items from the unordered list
    ul.innerHTML = '';

    // Add sorted list items back to the unordered list
    itemsArr.forEach(function (item) {
        ul.appendChild(item);
    });
}

function backwardClick() {
    currentPage = 1;
    createVisibleButton();
    showSuggestions(createFolderListData());
}

function leftClick() {
    currentPage--;
    createVisibleButton();
    showSuggestions(createFolderListData());
}

function rightClick() {
    currentPage++;
    createVisibleButton();
    showSuggestions(createFolderListData());
}

function forwardClick() {
    currentPage = totalPages;
    createVisibleButton();
    showSuggestions(createFolderListData());
}

function clickCurrentPage(pageNumber) {
    currentPage = pageNumber;
    createVisibleButton();
    showSuggestions(createFolderListData());
}

function licenseExpiryDialog(isExpired) {
    isLicenseExpired = isExpired;
    if (isLicenseExpired) {
        var licenseExpiredDiv = document.getElementById("licenseExpiryDialog");
        licenseExpiredDiv.style.visibility = "visible";
        return false;
    }
}

function disableBackButton() {

    window.history.forward();
}
// This method is to open file op to select local file
$(document).ready(function () {
    licenseExpiryDialog(isLicenseExpired);
    $('#fileOptions').change(function () {
        var val = $("#fileOptions option:selected").val();
        if (val == "Folder") {
            loadAzureFolder = false;
            loadLocalFolder();
        }
        //else if (val == "Azure") {
        //    loadAzureFolder = true;
        //    loadAzureFolders();
        //}
        $('#fileOptions').prop('selectedIndex', 0);
    });
    $('[data-toggle="tooltip"]').tooltip();
});