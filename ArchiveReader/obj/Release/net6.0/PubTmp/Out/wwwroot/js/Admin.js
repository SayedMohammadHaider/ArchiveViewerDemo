pageLoadSection();
window.history.forward();
function searchList(filterBy, inputId) {
    let input, filter, ul, li, txtValue;
    input = document.getElementById(inputId);
    filter = input.value.toUpperCase();
    ul = document.getElementById(filterBy);
    li = ul.getElementsByTagName("li");
    for (let i = 0; i < li.length; i++) {
        txtValue = li[i].textContent || li[i].innerText;
        if (txtValue.toUpperCase().indexOf(filter) > -1) {
            li[i].style.display = "";
        } else {
            li[i].style.display = "none";
        }
    }
}

function btnSyncFolders() {
    $.ajax({
        type: 'Get',
        async: true,
        contentType: "application/json;charset=utf-8",
        url: '/Home/syncFolders',
    });
    alert("Sync started");
}

function closeFolderDialog() {
    var notesHtml = document.getElementById("addFolderDialog");
    notesHtml.style.visibility = "hidden";
}

function addFolder() {
    showSpinner();
    var input = document.getElementById("addFolderInput");
    var inputText = input.value;
    $.ajax({
        type: "post",
        url: "/Home/AddFolder",
        data: { folderPath: inputText },
        datatype: "json",
        traditional: true,
        success: function (data) {
            alert(data);
            closeFolderDialog();
            console.log(data);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function loadAddFolderDialog() {
    showSpinner();
    var licenseExpiredDiv = document.getElementById("addFolderDialog");
    licenseExpiredDiv.style.visibility = "visible";
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: '/Home/readAllFolders',
        success: function (response) {
            var folderDetails = "";
            for (var j = 0; j < response.length; j++) {
                folderDetails += '<li>';
                folderDetails += '<a href="#">';
                folderDetails += '<span>' + response[j].title + '</span>';
                folderDetails += '<br>';
                folderDetails += '<span>' + response[j].path + '</span>';
                folderDetails += '</a>';
                folderDetails += '</li>';
                $('#allFolderList').html(folderDetails);
            }
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
    return false;
}

function pageLoadSection() {
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: '/Home/PageLoadSection',
        success: function (response)
        {
            var groupList = "";
            var secondSection = "";
            var thirdSection = "";
            groupList += '<h2>Groups</h2>';
            groupList += '<input class="searchClass" type="text" id="search-input-group" onkeyup=\'searchList("groupList","search-input-group")\' placeholder="Search Group">';
            groupList += '<ul id="groupList">';
            for (var j = 0; j < response.length; j++) {
                groupList += "<li><a onclick='getDetailsByGroup(\"" + response[j].id + "\")'>" + response[j].displayName + "</a></li>";
            }
            groupList += '</ul>';

            secondSection += '<h2>Folders</h2>';
            secondSection += '<input class="searchClass" type="text" id="search-input-folders" onkeyup=\'searchList("folderList","search-input-folders")\' placeholder="Search Folder">';
            secondSection += '<ul id="folderList">';
            secondSection += '</ul>';

            thirdSection += '<h2>Members</h2>';
            thirdSection += '<input class="searchClass" type="text" id="search-input-members" onkeyup=\'searchList("memberList","search-input-members")\' placeholder="Search Member">';
            thirdSection += '<ul id="memberList">';
            thirdSection += '</ul>';
            var groupSec = document.getElementById("firstSection");
            groupSec.style.width = "25%";
            var folderSec = document.getElementById("secondSection");
            folderSec.style.width = "50%";
            $('#firstSection').html(groupList);
            $('#secondSection').html(secondSection);
            $('#thirdSection').html(thirdSection);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function getDetailsByGroup(groupId) {
    showSpinner();
    $.ajax({
        type: "post",
        url: "/Home/getDetailByGroup",
        data: { groupId: groupId },
        datatype: "json",
        traditional: true,
        success: function (data)
        {
            var memberDetails = "";
            var folderDetails = "";
            var members = data.members;
            var folders = data.folders;
            for (var i = 0; i < members.length; i++) {
                memberDetails += ' <li class="member">';
                memberDetails += '<span class="member-name">' + members[i].displayName + '</span>';
                memberDetails += '<br>';
                if (members[i].mail == null) {
                    memberDetails += '<span class="member-email">' + members[i].userPrincipalName + '</span>';
                }
                else {
                    memberDetails += '<span class="member-email">' + members[i].mail + '</span>';
                }
                memberDetails += '</li>';
            }
            for (var j = 0; j < folders.length; j++) {
                folderDetails += '<li class="folderListli">';
                //folderDetails += '<a href="#">';
                folderDetails += '<span class="folderTitle">' + folders[j].title + '</span>';
                folderDetails += '<br>';
                folderDetails += '<span class="folderPath">' + folders[j].path + '</span>';
                //folderDetails += '</a>';
                folderDetails += '</li>';
            }
            if (folders.length == 0) {
                folderDetails = "No Folder found.";
            }
            if (members.length == 0) {
                memberDetails = "No Member found.";
            }
            $('#folderList').html(folderDetails);
            $('#memberList').html(memberDetails);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function getDetailsByFolder(folderId) {
    showSpinner();
    $.ajax({
        type: "post",
        url: "/Home/getDetailsByFolder",
        data: { folderId: folderId },
        datatype: "json",
        traditional: true,
        success: function (data)
        {
            var memberDetails = "";
            var groupList = "";
            for (var j = 0; j < data.length; j++) {
                groupList += "<li><a onclick='getMembersByGroup(\"" + data[j].groupId + "\")'>" + data[j].groupName + "</a></li>";
            }

            if (data.length == 0) {
                groupList = "No Group found.";
            }
            $('#groupList').html(groupList);
            $('#memberList').html(memberDetails);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function getMembersByGroup(groupId) {
    showSpinner();
    $.ajax({
        type: "post",
        url: "/Home/getMembersByGroup",
        data: { groupId: groupId },
        datatype: "json",
        traditional: true,
        success: function (data)
        {
            var memberDetails = "";           
            for (var i = 0; i < data.length; i++)
            {
                memberDetails += ' <li class="member">';
                memberDetails += '<span class="member-name">' + data[i].displayName + '</span>';
                memberDetails += '<br>';
                if (data[i].mail == null)
                {
                    memberDetails += '<span class="member-email">' + data[i].userPrincipalName + '</span>';
                }
                else
                {
                    memberDetails += '<span class="member-email">' + data[i].mail + '</span>';
                }
                memberDetails += '</li>';
            }
            if (data.length == 0) {
                memberDetails = "No member found.";
            }
            $('#memberList').html(memberDetails);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function filterByFolder() {
    showSpinner();
    $.ajax({
        type: 'Get',
        contentType: "application/json;charset=utf-8",
        url: '/Home/filterByFolder',
        success: function (response) {
            var firstSection = "";
            var secondSection = "";
            var thirdSection = "";
            firstSection += '<h2>Folders</h2>';
            firstSection += '<input class="searchClass" type="text" id="search-input-folders" onkeyup=\'searchList("folderList","search-input-folders")\' placeholder="Search Folder">';
            firstSection += '<ul id="folderList">';
            for (var j = 0; j < response.length; j++) {
                firstSection += '<li>';
                firstSection += "<a onclick='getDetailsByFolder(\"" + response[j].id + "\")'>";
                if (response[j].isParent) {
                    firstSection += '<span>' + response[j].path + '<i class="fa fa-trash deleteIcon" title="Remove" onclick="deleteToplevelFolder(\'' + response[j].id + '\')"></i></span>';
                }
                else {
                    firstSection += '<span>' + response[j].path +'</span>';
                }
                firstSection += '</a>';
                firstSection += '</li>';
            }
            firstSection += '</ul>';

            secondSection += '<h2>Groups</h2>';
            secondSection += '<input class="searchClass" type="text" id="search-input-group" onkeyup=\'searchList("groupList","search-input-group")\' placeholder="Search Group">';
            secondSection += '<ul id="groupList">';
            secondSection += '</ul>';

            thirdSection += '<h2>Members</h2>';
            thirdSection += '<input class="searchClass" type="text" id="search-input-members" onkeyup=\'searchList("memberList","search-input-members")\' placeholder="Search Member">';
            thirdSection += '<ul id="memberList">';
            thirdSection += '</ul>';
           
            var folderSec = document.getElementById("firstSection");
            folderSec.style.width = "50%";
            var groupSec = document.getElementById("secondSection");
            groupSec.style.width = "25%";

            $('#firstSection').html(firstSection);
            $('#secondSection').html(secondSection);
            $('#thirdSection').html(thirdSection);
            hideSpinner();
        },
        error: function (e, x) {
            hideSpinner();
        }
    });
}

function deleteToplevelFolder(folderId) {
    if (confirm("Are you sure you want to delete this folder?")) {
        $.ajax({
            type: "post",
            url: '/Home/deleteToplevelFolder',
            data: { folderId: folderId },
            datatype: "json",
            traditional: true,
            success: function (response) {
                showSpinner();
                filterByFolder();
            },
            error: function (e, x) {
                hideSpinner();
            }
        });
    }
    else {
        return false;
    }
}

function disableBackButton() {

    window.history.forward();
}

function showSpinner() {
    document.getElementById("spinner").style.display = "block";
}

// Method to hide spinner
function hideSpinner() {
    document.getElementById("spinner").style.display = "none";
}

$(document).ready(function () {
    $('#filterByGroupId').change(function () {
        var val = $("#filterByGroupId option:selected").val();
        if (val == "group") {
            pageLoadSection();
            $('#filterByGroupId').prop('selectedIndex', 0);
        }
        else if (val == "folder") {
            filterByFolder();
            $('#filterByGroupId').prop('selectedIndex', 1);
        }        
    });
});