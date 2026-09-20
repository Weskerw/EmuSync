# EmuSync

*Read it in English: [README.md](README.md)*

Tiene sincronizzati i salvataggi degli emulatori (memory card, save file) su tutti
i tuoi computer.

EmuSync usa due servizi, ognuno per quello che sa fare meglio:

- **Firebase** conserva il tuo *account* e la tua *configurazione*: quali
  emulatori sincronizzi, le etichette, le impostazioni e l'indice dei file
  sincronizzati.
- **Google Drive** conserva i *file di salvataggio veri e propri*, nel **tuo**
  Drive, dentro `Il mio Drive/EmuSync/<emulatore>/`. I salvataggi non passano
  dallo spazio di nessun altro e non consumano quota altrui.

I salvataggi sono sempre raggruppati **per emulatore**: PCSX2 finisce in
`EmuSync/pcsx2` su qualsiasi macchina, anche se in locale la cartella si chiama
in modo diverso. Accedi da un secondo PC ed EmuSync sa già quali emulatori
sincronizzi: deve solo trovarne le cartelle locali (e si offre di cercarle).

## Download

Scarica l'ultimo `EmuSync.exe` dalla
[pagina Releases](https://github.com/Weskerw/EmuSync/releases/latest).

È un unico eseguibile self-contained: niente installer e niente runtime .NET da
installare.

> Windows SmartScreen può mostrare "Windows ha protetto il PC" perché
> l'eseguibile non è firmato digitalmente. Clicca **Ulteriori informazioni →
> Esegui comunque**.

## Funzionalità

- **Account EmuSync** (Firebase Authentication): accesso con Google oppure con
  email + password. Accedendo con Google si autorizza Drive nello stesso
  passaggio, quindi il consenso nel browser è uno solo.
- **Catalogo emulatori con rilevamento automatico**: PCSX2, DuckStation, RPCS3,
  PPSSPP, Dolphin, Cemu, Ryujinx, yuzu, Citra, melonDS, mGBA, RetroArch,
  Flycast, Xenia, Project64, Snes9x — più l'opzione "personalizzato" per tutto
  il resto.
- **Configurazione condivisa tra dispositivi**: l'elenco degli emulatori sta
  nell'account, i percorsi locali restano per-dispositivo.
- Sincronizzazione automatica all'avvio, quando i salvataggi cambiano
  (`FileSystemWatcher` a eventi + 30 s di quiete, costo CPU/disco praticamente
  nullo) e periodica (di default ogni 15 min) per raccogliere le modifiche fatte
  su altri PC.
- **Le cancellazioni si propagano in sicurezza**: un indice dei file su
  Firestore distingue una cancellazione vera da un file semplicemente mai
  scaricato. I file rimossi finiscono nel cestino di Drive e nella cartella
  locale `%APPDATA%\EmuSync\trash`, mai direttamente nel nulla. Se la cartella
  locale è vuota, le cancellazioni non vengono mai propagate.
- Configurazione guidata al primo avvio, "Avvia con Windows" (nascosto nella
  tray, con azioni rapide sull'icona), data dell'ultima sincronizzazione per
  emulatore.

## Struttura del progetto

- **EmuSync.Core** — libreria .NET 8 multipiattaforma con tutta la logica:
  Firebase Auth e Firestore via REST (solo `HttpClient`), catalogo emulatori,
  client Drive e motore di sincronizzazione.
- **EmuSync** — interfaccia WinForms per Windows.
- **site/** — il sito pubblico e le regole di sicurezza di Firestore.

L'unico pezzo legato alla piattaforma è `IGoogleAuthorizationProvider`, cioè il
flusso di consenso OAuth. `DesktopGoogleAuthProvider` lo implementa per
Windows/Linux/macOS (browser di sistema + listener su loopback); una versione
Android ne aggiungerebbe una propria (Custom Tabs / AppAuth) riusando tutto il
resto invariato.

## Requisiti

- Visual Studio 2022 con il workload **Sviluppo di applicazioni desktop .NET** (.NET 8)
- Un account Google

## Configurare Firebase (solo sviluppatore, una volta sola, gratis)

**Gli utenti finali non devono fare niente di tutto questo.** Questi passaggi
identificano l'*applicazione* e si fanno una volta sola da chi la compila e la
distribuisce.

Tutto rientra comodamente nel piano gratuito **Spark**: su Firebase finiscono
solo piccoli documenti JSON (nessun file: quelli vanno su Drive).

1. Vai su <https://console.firebase.google.com/> e crea un progetto (o apri
   quello esistente `emusync-43d2b`). **Usa lo stesso progetto Google Cloud
   delle credenziali OAuth qui sotto**: Firebase accetta solo ID token Google
   emessi per un client OAuth del proprio progetto.
2. **Build → Authentication → Inizia**, poi abilita entrambi i provider:
   - **Email/Password**
   - **Google** (imposta l'email di supporto; i client OAuth del progetto sono
     considerati attendibili automaticamente)
3. **Build → Firestore Database → Crea database**, in modalità produzione, nella
   regione più vicina.
4. Pubblica le regole di sicurezza (limitano ogni documento al suo proprietario):
   ```
   cd site
   firebase deploy --only firestore:rules
   ```
5. **Impostazioni progetto → Generali → Le tue app → App web**: copia `apiKey` e
   `projectId`, poi in alternativa
   - mettili in `EmuSync.Core\FirebaseOptions.cs`
     (`EmbeddedApiKey` / `EmbeddedProjectId`), oppure
   - crea `EmuSync\emusync-firebase.json` (ignorato da git, ha la precedenza):
     ```json
     { "apiKey": "AIza...", "projectId": "emusync-43d2b" }
     ```

## Configurare le credenziali Google Drive (solo sviluppatore, una volta sola, gratis)

L'app usa l'API ufficiale di Google Drive con lo scope `drive.file` (vede solo i
file che ha creato lei, non tutto il tuo Drive).

1. Su <https://console.cloud.google.com/> seleziona lo **stesso progetto** di
   Firebase.
2. **API e servizi → Libreria**: abilita **Google Drive API**.
3. **API e servizi → Schermata consenso OAuth** (nelle console più recenti:
   **Google Auth Platform → Branding**): tipo di utente **Esterno**, compila i
   campi obbligatori e aggiungi il tuo indirizzo tra gli **utenti di test**
   finché l'app resta in modalità testing.
4. **API e servizi → Credenziali → Crea credenziali → ID client OAuth**, tipo di
   applicazione **App desktop**, poi scarica il JSON.
5. Copia `client_id` e `client_secret` in `EmuSync.Core\BuiltInCredentials.cs`,
   oppure metti il file, rinominato `credentials.json`, nella cartella `EmuSync\`
   accanto al `.csproj` (ignorato da git, ha la precedenza).

Il consenso per Drive richiede anche gli scope `openid`, `email` e `profile`: è
quello che produce l'ID token Google che EmuSync scambia con una sessione
Firebase, così "Continua con Google" copre entrambi i servizi in un colpo solo.

I token stanno in `%APPDATA%\EmuSync\`: quello di Drive sotto `token\`, quello di
Firebase in `session.json`, cifrato con DPAPI per l'utente Windows corrente.

**Distribuire l'app**: finché la schermata di consenso OAuth è in modalità
*Testing*, possono accedere con Google solo gli account elencati come utenti di
test. Premi **Pubblica app** per aprirla a tutti. Con il solo scope `drive.file`
non serve la verifica completa di Google, ma gli utenti potrebbero vedere un
avviso "app non verificata" superabile con *Avanzate → Vai a EmuSync*.

## Uso

1. Apri `EmuSync.sln` in Visual Studio e premi **F5**.
2. Primo avvio: accedi (Google o email), autorizza Drive, poi spunta gli
   emulatori che EmuSync ha trovato sul PC.
3. In seguito: **Sync → Add emulator...** per aggiungerne uno a mano,
   **Settings → Detect emulators on this PC...** per rifare la scansione,
   **Sync → Set local folder...** per collegare a una cartella locale un
   emulatore configurato su un altro dispositivo.
4. Tutto il resto è automatico. Nel menu **Sync** restano comunque
   **Sync selected** (F5) e **Sync all** (Ctrl+F5); il log mostra ogni
   operazione.

## Come decide cosa sincronizzare

Per ogni file EmuSync confronta tre stati: la copia locale, quella su Drive e
l'**indice** dell'ultima sincronizzazione (salvato su Firestore).

- nuovo in locale → caricato; nuovo su Drive → scaricato;
- contenuto identico (MD5) → non fa niente;
- modificato da un solo lato → vince quel lato, senza dover indovinare;
- modificato da entrambi i lati → vince la data di modifica più recente
  (tolleranza di 3 s per i filesystem FAT); se anche le date coincidono il
  conflitto viene segnalato e le due copie restano intatte;
- presente nell'indice ma sparito da un lato → lì è stato cancellato, quindi la
  cancellazione viene propagata (nel cestino di Drive, oppure in
  `%APPDATA%\EmuSync\trash` in locale). Se nel frattempo il file è cambiato
  dall'altro lato viene invece ripristinato.

Le date di modifica sono preservate in entrambe le direzioni, così il confronto
resta affidabile tra più PC.

## Dati salvati su Firestore

```
users/{uid}                     email, impostazioni, elenco emulatori
users/{uid}/devices/{deviceId}  nome del dispositivo e sue cartelle locali
users/{uid}/emulators/{key}     indice dei file (percorso, MD5, dimensione, data) e tombstone
users/{uid}/activity/{runId}    cronologia: un documento per sincronizzazione, con
                                tutti i file toccati — conservata un anno
```

Su Firebase non finisce nessun dato di salvataggio, solo metadati. Le regole di
sicurezza rendono ogni albero leggibile e scrivibile soltanto dal proprietario.

## Contribuire (repo pubblico)

Le credenziali **non sono nel repository**: `BuiltInCredentials.cs` è committato
con stringhe vuote, mentre `credentials.json` ed `emusync-firebase.json` sono in
`.gitignore`.

- **Per sviluppare/testare**: crea un tuo progetto Firebase e le tue credenziali
  OAuth (vedi sopra) e tienili nei due file ignorati da git.
- **Release ufficiali**: chi pubblica inserisce i valori in locale, compila e non
  committa la modifica.
- Non aprire mai pull request che contengano `client_id`/`client_secret` reali.
  Se finiscono in un commit per errore, rigenera subito il client secret nella
  Cloud Console (Credenziali → il tuo ID client → Reimposta secret).
  L'`apiKey` di Firebase non è un segreto (ogni app web la espone): a proteggere
  i dati sono le regole di sicurezza.

## Note e limiti

- Chiudi l'emulatore prima di sincronizzare a mano (i file di salvataggio
  potrebbero essere aperti in scrittura); la sincronizzazione automatica aspetta
  già 30 s di quiete.
- Rimuovere un emulatore non tocca nessun file, né in locale né su Drive.
- Uscendo dall'account la configurazione resta nel cloud: rientri e torna tutto.
- Per il desktop Linux/macOS basta una nuova interfaccia (Avalonia/GTK) sopra
  `EmuSync.Core`; Android richiede in più un proprio
  `IGoogleAuthorizationProvider` e la gestione dello scoped storage.

## Licenza

MIT — vedi [LICENSE](LICENSE).
