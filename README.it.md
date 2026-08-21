# EmuSync

*Read it in English: [README.md](README.md)*

Sincronizza i salvataggi dei tuoi emulatori (memory card, save file) con Google Drive.

Per ogni cartella configurata, EmuSync confronta i file locali con quelli su Drive
(cartella `EmuSync/<NomeProfilo>`): se il contenuto è identico (hash MD5) non fa
nulla, altrimenti vince il file con la data di modifica più recente, in entrambe
le direzioni.

## Funzionalità

- Profili multipli: una cartella per emulatore (PCSX2, Dolphin, ...), sottocartelle incluse
- Sincronizzazione automatica all'avvio
- Sincronizzazione automatica quando i salvataggi cambiano (`FileSystemWatcher`
  event-driven + 30 s di quiete, costo CPU/disco praticamente nullo)
- Controllo periodico di Drive (default ogni 15 min) per recepire modifiche da altri PC
- Data "ultima sincronizzazione" per profilo
- Accesso con Google una volta sola; il token resta salvato in locale
- Cambio account Google dal menu Impostazioni
- Configurazione iniziale guidata (accesso Google, scelta delle cartelle)
- Avvio automatico con Windows opzionale (menu Impostazioni): parte nascosto
  nella system tray, con azioni rapide (apri, sincronizza tutto, esci) sull'icona

## Struttura del progetto

- **EmuSync.Core** — libreria .NET 8 cross-platform con tutta la logica
  (client Google Drive, confronto, motore di sync). Riusabile per una futura
  versione Linux/Android (es. con Avalonia o MAUI).
- **EmuSync** — interfaccia grafica WinForms per Windows.

## Requisiti

- Visual Studio 2022 con il carico di lavoro **Sviluppo per desktop .NET** (.NET 8)
- Un account Google

## Configurare le credenziali Google (solo sviluppatore, una tantum, gratis)

**Gli utenti finali non devono fare nulla di tutto questo**: al primo avvio si
apre il browser con "Accedi con Google" e basta. Le credenziali qui sotto
identificano l'*applicazione* e vanno create una sola volta da chi la
compila/distribuisce.

L'app usa l'API ufficiale di Google Drive con permesso `drive.file`
(vede **solo** i file creati da lei stessa, non tutto il tuo Drive).

1. Vai su <https://console.cloud.google.com/> e accedi col tuo account Google.
2. Crea un nuovo progetto (nome libero, es. `EmuSync`).
3. Menu **API e servizi → Libreria**: cerca **Google Drive API** e premi **Abilita**.
4. Menu **API e servizi → Schermata consenso OAuth** (nelle console più recenti:
   **Google Auth Platform → Branding**):
   - tipo utente **Esterno**, compila solo i campi obbligatori (nome app, la tua email);
   - in **Utenti di test** aggiungi il tuo indirizzo Gmail (l'app può restare in modalità test).
5. Menu **API e servizi → Credenziali → Crea credenziali → ID client OAuth**:
   - tipo di applicazione: **App desktop**;
   - scarica il file JSON.
6. Apri il JSON scaricato e copia `client_id` e `client_secret` nelle due costanti
   di `EmuSync.Core\BuiltInCredentials.cs`: da quel momento l'app è autonoma e
   chiunque la usi deve solo accedere con Google.
   (In alternativa puoi mettere il file, rinominato `credentials.json`, nella
   cartella `EmuSync\` accanto al `.csproj`: se presente ha la precedenza.)

Alla prima sincronizzazione si aprirà il browser per autorizzare l'app;
il token viene salvato in `%APPDATA%\EmuSync\token` e non verrà più richiesto.

**Per distribuire l'app ad altri**: finché la schermata consenso OAuth è in
modalità *Test*, possono accedere solo gli account elencati come utenti di test.
Per aprirla a tutti, premi **Pubblica app** nelle impostazioni della schermata
consenso. Con il solo scope `drive.file` non serve la verifica completa di
Google, ma gli utenti potrebbero vedere un avviso "app non verificata"
superabile con *Avanzate → Vai a EmuSync*.

## Uso

1. Apri `EmuSync.sln` in Visual Studio e premi **F5**.
2. **Add folder...** → scegli la cartella dei salvataggi dell'emulatore
   (es. `memcards` di PCSX2, `GC`/`Wii` di Dolphin, ecc.) e dalle un nome.
3. Puoi aggiungere quante cartelle vuoi, una per emulatore.
4. Il resto è automatico: sync all'avvio, quando i salvataggi cambiano e
   periodicamente per le modifiche remote. Restano i pulsanti manuali
   **Sync selected** / **Sync all**. Il log mostra ogni operazione.

Su Drive i file finiscono in `Il mio Drive/EmuSync/<NomeProfilo>/...`,
sottocartelle comprese.

## Come decide cosa sincronizzare

Per ogni file (unione di locale e remoto):

- presente solo in locale → viene caricato;
- presente solo su Drive → viene scaricato;
- presente in entrambi → se l'MD5 coincide non fa nulla; altrimenti vince la
  data di modifica più recente (tolleranza 3 s per i filesystem FAT).
  Se le date coincidono ma il contenuto è diverso, segnala il conflitto e salta il file.

Le date di modifica vengono preservate in entrambe le direzioni, quindi il
confronto resta affidabile anche tra più PC.

## Contribuire (repo pubblico)

Le credenziali OAuth **non sono nel repository**: `BuiltInCredentials.cs` è
committato con le stringhe vuote e `credentials.json` è nel `.gitignore`.

- **Per sviluppare/testare**: crea le tue credenziali di test nella Google Cloud
  Console (vedi sopra, ~5 minuti) e metti il JSON scaricato, rinominato
  `credentials.json`, nella cartella `EmuSync\` accanto al `.csproj`.
  Il codice lo usa automaticamente e git lo ignora.
- **Per le release ufficiali**: chi pubblica compila i valori in
  `BuiltInCredentials.cs` solo in locale, genera la build e non committa la modifica.
- Non aprire mai pull request che contengano `client_id`/`client_secret` reali.
  Se per errore vengono committati, rigenera subito il client secret nella
  Cloud Console (Credenziali → il tuo ID client → Reimposta segreto).

## Note e limiti

- Chiudi l'emulatore prima di sincronizzare manualmente (file di salvataggio
  in scrittura); la sync automatica aspetta già 30 s di quiete.
- La rimozione di un profilo non tocca i file, né locali né su Drive.
- I file cancellati localmente vengono riscaricati da Drive (le cancellazioni
  non vengono propagate: per i salvataggi è la scelta più sicura).
- Il futuro supporto Linux desktop richiede solo una nuova UI (Avalonia/GTK)
  sopra `EmuSync.Core`; per Android servirà anche un flusso OAuth dedicato.
