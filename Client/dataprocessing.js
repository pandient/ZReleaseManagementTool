var products = [], versions = [], fileNames = [];
var currentProduct = '';
var currentVersion = '';
var currentFile = '';

window.onload = function(){
	getProducts();
}

var iconsList = ["icon icon-doc-text-inv", "icon icon-vcard", "icon icon-download",
		"icon icon-doc", "icon icon-newspaper", "icon icon-folder", "icon icon-docs",
		"icon icon-archive", "icon icon-box", "icon icon-bag"];

function uintToString(uintArray) {
	return String.fromCharCode.apply(null, uintArray);
}

function setActiveItem(item) {
	navigationClass = "nav-group-item";
	if (item.classList != 0) {
		var elements = document.getElementsByClassName(navigationClass);
		for (var i=0; i<elements.length; i++) {
			elements[i].style.backgroundColor = "#f5f5f4";
		}
	} else {
		var elements = document.getElementById("table1").children[0].children;
		for (var i=0; i<elements.length; i++) {
			elements[i].style.backgroundColor = "white";
		}
	}
	item.style.backgroundColor = "#dcdfe1";
}

function displayProducts(data) {
	products = uintToString(data.slice(64)).split('\r');
	document.getElementById("table2").innerHTML = '';
	var elements = document.getElementsByClassName("nav-group");	
	for (var key in products) {
		var span = document.createElement("span");
		if (key == 0) {
			span.setAttribute("class", "nav-group-item");
		} else {
			span.setAttribute("class", "nav-group-item");
		}
		var icon = document.createElement("span");
		icon.setAttribute("class", iconsList[key]);
		span.innerHTML = products[key];
		span.addEventListener("click", function(handle) {
			currentProduct = this.innerText;
			setActiveItem(this);
			getVersions(currentProduct);
		});
		elements[0].appendChild(span);
		span.appendChild(icon);
	}
	//callGetVersions();
}
//don't need this
function callGetVersions() {
	if (!connectionActive) {
		getVersions(products[0]);
	} else {
		setTimeout(function() {
			callGetVersions();
		}, 100);
	}
}

function displayVersions(data) {
	versions = uintToString(data.slice(64)).split('\r');
	document.getElementById("table1").innerHTML = '';
	document.getElementById("table2").innerHTML = '';
	var tableBody = document.createElement("tbody");
	for (var key in versions) {
		var row = document.createElement("tr");
		var cell = document.createElement("td");
		cell.innerHTML = versions[key];
		row.appendChild(cell);
		row.addEventListener("click", function(handle) {
			currentVersion = this.innerText;
			setActiveItem(this);
			// checkAlert(currentProduct, currentVersion, function() {
				// getFileList(currentProduct, currentVersion);
			// });
			getFileList(currentProduct, currentVersion);
			
		});
		tableBody.appendChild(row);
	}
	document.getElementById("table1").appendChild(tableBody);
	//callGetFilesList();
}

// don't use this
function callGetFilesList() {
	if (!connectionActive) {
		getFileList(products[0], versions[0]);
	} else {
		setTimeout(function() {
			callGetFilesList();
		}, 100);
	}
}

function displayFileList(data) {
	console.log(data);
	fileNames = uintToString(data.slice(64)).split('\r');
	document.getElementById("table2").innerHTML = '';
	var tableBody = document.createElement("tbody");
	for (var key in fileNames) {
		var row = document.createElement("tr");
		var cell = document.createElement("td");
		cell.innerHTML = fileNames[key];
		row.appendChild(cell);
		tableBody.appendChild(row);
		row.addEventListener("click", function(handle) {
			currentFile = this.innerText;
			checkAlert(currentProduct, currentVersion, function() {
					getFile(currentProduct, currentVersion ,currentFile);
			 });
			//getFile(currentProduct, currentVersion ,currentFile);
		});
	}
	document.getElementById("table2").appendChild(tableBody);
}

function saveFile(data) {
	var fs = require('fs');
	const dialog = require('electron').remote.dialog;
	var options = {
		title : 'Save as',
		defaultPath : saveFile.fileName,
		filters : [
		 	{ name: 'All Files', extensions: ['*'] },
			{ name: 'Archive', extensions: ['zip','rar', '7z'] }
		]		
	};
	dialog.showSaveDialog(options,function(name) {
		fs.writeFile(name, data.slice(64) , (err) => {
				if (err) throw err;
				console.log('It\'s saved!');
				alert('It\'s saved!', 'ZE');
		});
	});
}

function displayAlert(data){
	var status = data.readInt32LE(40);
	if (!status) {
	    checkAlert.callback();
	}
	else {
	    alert(data.slice(64), "Alert");
	}
}

function processResponse(data) {
    var reqId = data.readInt32LE(4);
	console.log(reqId);
    switch (reqId) {
        case PRODUCTS:
            displayProducts(data);
            break;
        case VERSIONS:
            displayVersions(data);
            break;
		case FILE_LIST:
            displayFileList(data);
            break;	
		case DOWNLOAD_FILE:
            saveFile(data);
            break;		
		case ALERT:
            displayAlert(data);
            break;
        default:
            break;
    }
}