var net = require('net');
var buffer = require('buffer');
var reqBuff;
var respBuff;

const PRODUCTS = 1;
const VERSIONS = 2;

var client = new net.Socket();

function connect() {
    if (client.readyState != 'closed') {
        return;
    }
    client.connect(10001, '10.0.1.23', function() {
        console.log('Connected');
    });

    client.on('data', function (data) {
        console.log('Received: ' + data);
        if (client.bufferSize == 0) {
            client.messageSize = data.readInt32LE(0);
        }
        client.messageReadSize += data.length;
        respBuff = Buffer.concat([respBuff, data], respBuff.length + data.length);

        if (client.messageReadSize >= client.messageSize + 64) {
            processResponse(respBuff);
            //displayProducts(respBuff);
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

function resetBuffer() {
    reqBuff = new buffer.Buffer(64);
    client.messageSize = 0;
    client.messageReadSize = 0;
    respBuff = new buffer.Buffer(0);
}

function getProducts() {
    connect();
    resetBuffer();
	    
	reqBuff.writeUInt8(PRODUCTS, 7);
	reqBuff.write('tony', 8);
	client.write(reqBuff);
}

function getVersions() {
    var productName = getProductName();
    connect();
    resetBuffer();

    reqBuff.writeUInt8(productName.length, 3);
    reqBuff.writeUInt8(VERSIONS, 7);
    reqBuff.write('tony', 8);
    reqBuff = Buffer.concat([reqBuff, new buffer.Buffer(productName)], reqBuff.length + productName.length);
    client.write(reqBuff);

}

function getProductName() {
    var input = document.getElementById('productName');
    return input.value;
}

function displayProducts(data){
	var div = document.getElementById('products');
	div.innerHTML = data.slice(64);
}

function displayVersions(data) {

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
        default:
            break;
    }
}
