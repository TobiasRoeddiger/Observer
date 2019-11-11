'use strict';

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (changeInfo.url) {
    fetch("http://localhost:8080/url/", {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ url: changeInfo.url })
    })
    .catch(() => {
      console.log(changeInfo.url);
    });
  }
});
