var net = require('net');
var buffer = require('buffer');
var buff;
var respBuff;

  function connect () {
        var client = new net.Socket();
	    buff = new buffer.Buffer(64);
	    client.messageSize = 0;
	    client.messageReadSize = 0;
	    respBuff = new buffer.Buffer(0);
	
	    client.connect(10001, '10.0.1.23', function() {
		console.log('Connected');
		buff.writeUInt8(0x1,7);
		buff.write('tony', 8 );
		client.write(buff);
		
	});

	client.on('data', function(data) {
	    console.log('Received: ' + data);
	    if(client.bufferSize == 0){
		    client.messageSize = data.readInt32LE(0);
	    }
	    client.messageReadSize += data.length;
	    respBuff = Buffer.concat([respBuff, data], respBuff.length + data.length);
		
	    if(client.messageReadSize >= client.messageSize + 64){
		    displayProducts(respBuff);
	    }
		
	    //client.destroy(); // kill client after server's response
		
		
	});


	client.on('close', function() {
		console.log('Connection closed');
	});
	
	client.on('error', function(error) {
		console.log('Error' + error);
		alert(error);
	});

}

function displayProducts(data){
	
	var div = document.getElementById('products');
	div.innerHTML = data.slice(64);
}
