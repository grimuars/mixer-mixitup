var connection;
var isDebug = false;

function openWebsocketConnection() {
    openWebsocketConnectionWithAddress(window.location.hostname, window.location.port);
}

function openWebsocketConnectionWithPort(port) {
    openWebsocketConnectionWithAddress(window.location.hostname, port);
}

function openWebsocketConnectionWithAddress(address, port) {
    try {
        var protocol = "ws";
        if (window.location.protocol === "https:") {
            protocol = "wss";
        }
        connection = new WebSocket(protocol + "://" + address + ":" + port + "/ws/");

        // When the connection is open, send some data to the server
        connection.onopen = function () {
            connectionOpened();
            sendPacket('ping', '');
        };

        // Log messages from the server
        connection.onmessage = function (e) {
            var packet = getDataFromJSON(e.data);
            if (packet !== null && typeof packet.type !== 'undefined') {
                if (packet.type === "debug") {
                    isDebug = true;
                }
                else if (packet.type === "test") {
                    sendPacket('test', '');
                }
            }
            packetReceived(packet);
        };

        // Log errors
        connection.onerror = function (error) {
            logToSessionStorage('WebSocket Error ' + error.toString());
        };

        connection.onclose = function (e) {
            connectionClosed();
            setTimeout(function () { openWebsocketConnection(port); }, 1000);
        };
    }
    catch (err) { logToSessionStorage(err); }
}

function sendPacket(type, data) {
    try {
        if (connection !== null && connection.readyState === WebSocket.OPEN) {
            var packet = { type: type, data: data };
            connection.send(JSON.stringify(packet));
        }
    }
    catch (err) { logToSessionStorage(err); }
}

function getDataFromJSON(packet) {
    try {
        if (packet !== null) {
            return JSON.parse(packet);
        }
    }
    catch (err) { logToSessionStorage(err); }
    return null;
}

function isStringEmpty(str) {
    return (!str || 0 === str.length);
}

function logToSessionStorage(log) {
    try {
        // Disabling session storage logging unless needed for diagnosing issues.
        //
        //if (typeof (Storage) !== "undefined") {
        //    if (!sessionStorage.logs) {
        //        sessionStorage.logs = "";
        //    }
        //    sessionStorage.logs += "\n\n" + log.toString();

        //    console.log(log.toString());
        //}
    }
    catch (err) { }
}