var net = require('net');
var buffer = require('buffer');
var reqBuff;
var respBuff;

const PRODUCTS = 1;
const VERSIONS = 2;
const FILE_LIST = 4;
const DOWNLOAD_FILE = 5;
const ALERT = 6;

var USER = 'saad';



var client = new net.Socket();
init();

function init() {
	client.on('data', function (data) {
			console.log('Received: ' + data);
			if (client.messageReadSize == 0) {
				client.messageSize = data.readInt32LE(0);
				//client.messageSize = 20;
				console.log(client.messageSize);
			}
			client.messageReadSize += data.length;
			respBuff = Buffer.concat([respBuff, data], respBuff.length + data.length);

			if (client.messageReadSize >= client.messageSize + 64) {
				processResponse(respBuff);
				//displayProducts(respBuff);
				client.destroy();
			}

			//client.destroy(); // kill client after server's response
		});

		client.on('close', function () {
			console.log('Connection closed');
		});

		client.on('error', function (error) {
			console.log('Error' + error);
			alert(error);
		});
}

function connect() {
    if (client.readyState != 'closed') {
        return;
    }
    client.connect(10001, '10.0.1.207', function() {
        console.log('Connected');
    });

    
}

function resetBuffer() {
    reqBuff = new buffer.Buffer(64);
    client.messageSize = 0;
    client.messageReadSize = 0;
    respBuff = new buffer.Buffer(0);
}

function isBusy() {
    return client.readyState != 'closed';
}

function getProducts() {
 
    connect();
    resetBuffer();
	    
	// reqBuff.writeUInt32BE(0, 0);
	// reqBuff.writeUInt32BE(PRODUCTS,4);
	// reqBuff.write('tony', 8,32);
	setHeader(PRODUCTS,USER,'');
	client.write(reqBuff);
}

function getVersions(product) {
  
    var productName = product;
    connect();
    resetBuffer();

    reqBuff.writeUInt32BE(productName.length, 0);
    reqBuff.writeUInt32BE(VERSIONS, 4);
    reqBuff.write('tony', 8,32);
    reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(productName)], reqBuff.length + productName.length);
    client.write(reqBuff);

}

function getFileList(product, version) {
  
	var productName = product;
    var versionName = version;
	
    connect();
    resetBuffer();

    reqBuff.writeUInt32BE(versionName.length + productName.length + 1, 0);
    reqBuff.writeUInt32BE(FILE_LIST, 4);
    reqBuff.write('tony', 8,32);
    reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(productName + '\r' + versionName)], reqBuff.length + productName.length + versionName.length + 1);
    client.write(reqBuff);

}

function getFile(product, version, file) {
  
    var productName = product;
    var versionName = version;
	var fileName = file;
	saveFile.fileName = file;
	
	var query = productName + '\r' + versionName + '\r' +  fileName;
    connect();
    resetBuffer();

    // reqBuff.writeUInt32BE(query.length, 0);
    // reqBuff.writeUInt32BE(DOWNLOAD_FILE, 4);
    // reqBuff.write('tony', 8,32);
    // reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(query)], reqBuff.length + query.length);
	setHeader(DOWNLOAD_FILE,USER,query);
    client.write(reqBuff);

}

function  checkAlert(product, version) {

	var productName = product;
    var versionName = version;
	
	var query = productName + '\r' + versionName;
    connect();
    resetBuffer();
	
	setHeader(ALERT,USER,query);
    client.write(reqBuff);

}


function setHeader(reqId,user,query){
	reqBuff.writeUInt32BE(query.length, 0);
    reqBuff.writeUInt32BE(reqId, 4);
    reqBuff.write(user, 8,32);
    reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(query)], reqBuff.length + query.length);

}



function getProductName() {
    var input = document.getElementById('productName');
    return input.value;
}




