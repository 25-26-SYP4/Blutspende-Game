const http = require('http');
const fs = require('fs').promises;
const path = require('path');

const PORT = 3000;
const SCOREBOARD_FILE = path.join(__dirname, 'scoreboard.json');

// ── Logger ────────────────────────────────────────────────────────────────────
function log(level, message, data = null) {
    const timestamp = new Date().toISOString();
    const prefix = {
        INFO:    '\x1b[36m[INFO]\x1b[0m ',
        SUCCESS: '\x1b[32m[OK]  \x1b[0m ',
        WARN:    '\x1b[33m[WARN]\x1b[0m ',
        ERROR:   '\x1b[31m[ERR] \x1b[0m ',
    }[level] || '[LOG] ';

    console.log(`${timestamp} ${prefix} ${message}`);
    if (data) console.log('                  ', JSON.stringify(data, null, 2));
}
// ─────────────────────────────────────────────────────────────────────────────

async function loadScoreboard() {
    try {
        const data = await fs.readFile(SCOREBOARD_FILE, 'utf8');
        const parsed = JSON.parse(data);
        log('INFO', `Scoreboard geladen (${Object.keys(parsed).length} Einträge)`);
        return parsed;
    } catch (error) {
        log('WARN', 'Scoreboard-Datei nicht gefunden, starte leer.');
        return {};
    }
}

async function saveScoreboard(scoreboard) {
    try {
        await fs.writeFile(SCOREBOARD_FILE, JSON.stringify(scoreboard, null, 2));
        log('SUCCESS', `Scoreboard gespeichert (${Object.keys(scoreboard).length} Einträge)`);
    } catch (error) {
        log('ERROR', 'Fehler beim Speichern: ' + error.message);
        throw error;
    }
}

function parseBody(req) {
    return new Promise((resolve, reject) => {
        let body = '';
        req.on('data', chunk => { body += chunk.toString(); });
        req.on('end', () => {
            try { resolve(JSON.parse(body)); }
            catch (error) {
                log('ERROR', 'Body konnte nicht geparst werden: ' + error.message);
                reject(error);
            }
        });
    });
}

const server = http.createServer(async (req, res) => {
    const clientIp = req.socket.remoteAddress;
    log('INFO', `${req.method} ${req.url} von ${clientIp}`);

    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    res.setHeader('Content-Type', 'application/json');

    // Preflight
    if (req.method === 'OPTIONS') {
        log('INFO', 'Preflight OPTIONS beantwortet');
        res.statusCode = 204;
        res.end();
        return;
    }

    // POST /score
    if (req.method === 'POST' && req.url === '/score') {
        try {
            const body = await parseBody(req);
            const { id, name, score } = body;

            log('INFO', 'Score-Update empfangen:', { id, name, score });

            if (!id || !name || score === undefined) {
                log('WARN', 'Ungültige Daten – id, name oder score fehlt', body);
                res.statusCode = 400;
                res.end(JSON.stringify({ error: 'id, name und score sind erforderlich' }));
                return;
            }

            const scoreboard = await loadScoreboard();
            const isNew = !scoreboard[id];

            scoreboard[id] = { name, score, timestamp: new Date().toISOString() };
            await saveScoreboard(scoreboard);

            log('SUCCESS', `Score ${isNew ? 'neu erstellt' : 'aktualisiert'}: [${id}] ${name} = ${score}`);

            res.statusCode = 200;
            res.end(JSON.stringify({ success: true, message: 'Score gespeichert', data: { id, name, score } }));

        } catch (error) {
            log('ERROR', 'Fehler bei POST /score: ' + error.message);
            res.statusCode = 500;
            res.end(JSON.stringify({ error: 'Serverfehler: ' + error.message }));
        }
    }

    // GET /scoreboard
    else if (req.method === 'GET' && req.url === '/scoreboard') {
        try {
            const scoreboard = await loadScoreboard();
            const result = Object.entries(scoreboard).map(([id, data]) => ({
                id, name: data.name, score: data.score
            }));

            result.sort((a, b) => b.score - a.score);

            log('SUCCESS', `Scoreboard abgerufen – ${result.length} Einträge gesendet`);
            if (result.length > 0) {
                log('INFO', 'Top 3:', result.slice(0, 3));
            }

            res.statusCode = 200;
            res.end(JSON.stringify(result));

        } catch (error) {
            log('ERROR', 'Fehler bei GET /scoreboard: ' + error.message);
            res.statusCode = 500;
            res.end(JSON.stringify({ error: 'Serverfehler: ' + error.message }));
        }
    }

    else {
        log('WARN', `Unbekannter Endpoint: ${req.method} ${req.url}`);
        res.statusCode = 404;
        res.end(JSON.stringify({ error: 'Endpoint nicht gefunden' }));
    }
});

server.listen(PORT, () => {
    log('SUCCESS', `Scoreboard Server läuft auf http://localhost:${PORT}`);
    log('INFO', 'Endpoints: POST /score | GET /scoreboard');
});