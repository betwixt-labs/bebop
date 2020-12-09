let preElem = document.querySelector('.logo-code');
let originalCode = preElem.innerText;

let newCharacters = ['0', '1'];
function animateCode() {
    preElem.innerText = originalCode;
    let newCode = '';

    var s = preElem.innerText.split('');
    for (var i = 0; i < s.length; i++) {
        if (Math.random() > 0.1) { 
            newCode += s[i];
            continue;
        }
        if (s[i] == ' ' || s[i] == '\n') {
            newCode += s[i];
        }
        else {
            s[i] = newCharacters[Math.floor(Math.random() * newCharacters.length)];
            newCode += s[i];
        }
    }

    preElem.innerText = newCode;
}

setInterval(animateCode, 200);