document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    showHideSettings(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            showHideSettings(payload.settings);
        }
    });
});

function showHideSettings(payload) {
    console.log("Show Hide Settings Called");

    if (payload['muteDevice'])
        document.getElementById('rdMute').style.display = 'block';
    if (payload['adjustVolume'])
        document.getElementById('step-item').style.display = 'block';
    if (payload['setVolume'])
        document.getElementById('set-item').style.display = 'block';
    if (payload['actionSet'])
        document.getElementById('actionSetDiv').style.display = 'block';
    if (payload['actionSwitch'])
        document.getElementById('actionSwitchDiv').style.display = 'block';
    if (payload['modeTarget'] == 1)
        document.getElementById("selectClassic").style.display = 'block';
    if (payload['modeTarget'] == 2)
        document.getElementById("selectStream").style.display = 'block';
}
function updateDisplay(source, data)
{
    if (source == "volume") {
        document.getElementById('rdMute').style.display = 'none';
        document.getElementById('step-item').style.display = 'none';
        document.getElementById('set-item').style.display = 'none';
        document.getElementById(data.getAttribute("switchtarget")).style.display = 'block';
    }
    else if (source == 'deviceSwitch') {
        document.getElementById('actionSetDiv').style.display = 'none';
        document.getElementById('actionSwitchDiv').style.display = 'none';
        document.getElementById(data.getAttribute("switchtarget")).style.display = 'block';
    }
    else if (source == 'modeSwitch') {
        document.getElementById('selectClassic').style.display = 'none';
        document.getElementById('selectStream').style.display = 'none';
        if (data.value == 1)
            document.getElementById("selectClassic").style.display = 'block';
        else if (data.value == 2)
            document.getElementById("selectStream").style.display = 'block';
    }
}

function refreshDevice() {
    var payload = {};
    payload.property_inspector = 'refreshDevice';
    sendPayloadToPlugin(payload);
}