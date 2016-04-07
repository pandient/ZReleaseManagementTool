var UDP_PORT = 5001;
var UDP_HOST = '10.0.1.207';
var dgram = require('dgram');
var udp_client = dgram.createSocket('udp4');


udp_client.on('listening', function () {
    var address = udp_client.address();
    console.log('UDP Client listening on ' + address.address + ":" + address.port);
    udp_client.setBroadcast(true)
    udp_client.setMulticastTTL(128); 
    udp_client.addMembership('230.0.0.1');
});

udp_client.on('message', function (message, remote) {   
    console.log('A: Epic Command Received. Preparing Relay.');
    console.log('B: From: ' + remote.address + ':' + remote.port +' - ' + message);
	addMessageToPanel(message);
});

udp_client.bind(UDP_PORT, UDP_HOST);

function addMessageToPanel(message){
	var panel = document.getElementById('messages');
	var html = '  <li  class="list-group-item">' +
 
    '<div class="media-body">' +
      
      '<p>' + message + '</p>' +
    '</div></li>' ;
	panel.innerHTML = html + panel.innerHTML;
  
	
}