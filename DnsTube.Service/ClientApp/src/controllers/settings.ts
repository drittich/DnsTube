import '../style.css'
import 'bootstrap/dist/css/bootstrap.min.css'
import { getDbFolderAsync, getSettingsAsync, saveSettingsAsync } from "../services/Settings";

init();

function init() {
	getSettings();
	showDbFolder();

	document.getElementById('saveSettings')!.addEventListener('click', function (e) {
		e.preventDefault();
		(document.getElementById('saveSettings') as (HTMLButtonElement)).disabled = true;

		var form = document.getElementById('settings')! as HTMLFormElement;
		form.style.display = 'none';
		var processing = document.createElement('div');
		processing.appendChild(document.createTextNode('processing ...'));
		form.parentNode!.insertBefore(processing, form);

		var formData = new FormData(form);
		saveSettingsAsync(formData)
			.then(() => {
				location.reload();
			});
	}, false);

	let authTypeRadioButtons = document.getElementsByName("IsUsingToken");
	for (let i = 0; i < authTypeRadioButtons.length; i++) {
		authTypeRadioButtons[i].addEventListener("click", function () {
			setAuthLabel((this as HTMLInputElement).value == 'true');
		}, false);
	}
}

async function getSettings(): Promise<void> {
	let settings = await getSettingsAsync();

	if (settings == null)
		return;

	(document.getElementById('emailAddress') as (HTMLInputElement)).value = settings.emailAddress!;
	(document.getElementById('authorizationToken') as (HTMLInputElement)).checked = settings.isUsingToken!;
	(document.getElementById('authorizationKey') as (HTMLInputElement)).checked = !settings.isUsingToken!;
	(document.getElementById('apiKeyOrToken') as (HTMLInputElement)).value = settings.apiKeyOrToken!;
	(document.getElementById('zoneIDs') as (HTMLInputElement)).value = settings.zoneIDs!;

	(document.getElementById('protocolIpv4') as (HTMLInputElement)).checked = settings.protocolSupport == 0;
	(document.getElementById('protocolIpv6') as (HTMLInputElement)).checked = settings.protocolSupport == 1;
	(document.getElementById('protocolBoth') as (HTMLInputElement)).checked = settings.protocolSupport == 2;

	(document.getElementById('ipv4Api') as (HTMLInputElement)).value = settings.iPv4_API!;
	(document.getElementById('ipv6Api') as (HTMLInputElement)).value = settings.iPv6_API!;
	(document.getElementById('updateIntervalMinutes') as (HTMLInputElement)).value = settings.updateIntervalMinutes?.toString()!;
	(document.getElementById('notifyOfUpdates') as (HTMLInputElement)).checked = !settings.skipCheckForNewReleases!;

	setAuthLabel(settings.isUsingToken!)
}

function setAuthLabel(isUsingToken: boolean) {
	document.getElementById('apiKeyTokenLabel')!.innerText = isUsingToken ? 'Authorization Token' : 'Authorization Key';
}
async function showDbFolder() {
	let dbFolder = await getDbFolderAsync();
	document.getElementById('dbPath')!.innerText = dbFolder!;
}

