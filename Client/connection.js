var net = require('net');
var buffer = require('buffer');
var reqBuff;
var respBuff;
var connectionActive = false;

const SERVER_IP = '10.0.1.207';
const SERVER_PORT = 10001;
const PRODUCTS = 1;
const VERSIONS = 2;
const FILE_LIST = 4;
const DOWNLOAD_FILE = 5;
const ALERT = 6;

var USER = 'saad';
var client = new net.Socket();

client.on('data', function (data) {
	console.log('Received: ' + data);
	
	if (client.messageReadSize == 0) {
		client.messageSize = data.readInt32LE(0);
		console.log(client.messageSize);
	}
	client.messageReadSize += data.length;
	respBuff = Buffer.concat([respBuff, data], respBuff.length + data.length);
	if (client.messageReadSize >= client.messageSize + 64) {
		var status = respBuff.readInt32LE(40)
		if(status){
			console.log( 'error status  ' + respBuff.readInt32LE(40));
		}
	    
		processResponse(respBuff);
		//client.destroy();
	}
	//client.destroy(); // kill client after server's response
});

client.on('close', function () {
	console.log('Connection closed');
	connectionActive = false;
});

client.on('error', function (error) {
	console.log('Error' + error);
	alert(error);
});


function connect() {
    if (client.readyState != 'closed') {
        return;
    }
    client.connect(SERVER_PORT, SERVER_IP, function() {
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
	connectionActive = true;
    connect();
    resetBuffer();
	setHeader(PRODUCTS,USER,'');
	client.write(reqBuff);
}

function getVersions(product) {
	connectionActive = true;
    connect();
    resetBuffer();
    setHeader(VERSIONS,USER,product);
    client.write(reqBuff);
}

function getFileList(product, version) {
	connectionActive = true;
    connect();
    resetBuffer();
	setHeader(FILE_LIST,USER,product + '\r' + version);
    client.write(reqBuff);
}

function getFile(product, version, file) {
	connectionActive = true;
	saveFile.fileName = file;
	var query = product + '\r' + version + '\r' +  file;
    connect();
    resetBuffer();
	setHeader(DOWNLOAD_FILE,USER,query);
    client.write(reqBuff);
}

function  checkAlert(product, version , callback) {
	connectionActive = true;
	var query = product + '\r' + version;
    connect();
    resetBuffer();
	setHeader(ALERT,USER,query);
    client.write(reqBuff);
	checkAlert.callback = callback;
}

function setHeader(reqId,user,query){
	reqBuff.writeUInt32BE(query.length, 0);
    reqBuff.writeUInt32BE(reqId, 4);
    reqBuff.write(user, 8,32);
    reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(query)], reqBuff.length + query.length);
}