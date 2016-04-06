

function displayProducts(data){
	var div = document.getElementById('products');
	div.innerHTML = data.slice(64);
}

function displayVersions(data) {
	var div = document.getElementById('versions');
	div.innerHTML = data.slice(64);
}

function displayFileList(data) {
	var div = document.getElementById('fileList');
	div.innerHTML = data.slice(64);
}

function saveFile(data) {
	//
	var fs = require('fs');
	
	fs.writeFile('C:\\Workspace\\test.zip', data.slice(64) , (err) => {
			if (err) throw err;
			console.log('It\'s saved!');
	});
}


function processResponse(data) {
    var reqId = data.readInt32LE(4);
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
			
        default:
            break;
    }
}
