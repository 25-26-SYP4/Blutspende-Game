import http.server
import threading
import socket
import os
import subprocess
import sys
import tkinter as tk

PORT = 8080

# ── Eingebetteter Node.js REST-Server (server.js) ─────────────────────────────
NODE_SERVER_CODE = r"""
const http = require('http');
const fs = require('fs').promises;
const path = require('path');

const PORT = 3000;
const SCOREBOARD_FILE = path.join(__dirname, 'scoreboard.json');

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

async function loadScoreboard() {
    try {
        const data = await fs.readFile(SCOREBOARD_FILE, 'utf8');
        const parsed = JSON.parse(data);
        log('INFO', `Scoreboard geladen (${Object.keys(parsed).length} Eintraege)`);
        return parsed;
    } catch (error) {
        log('WARN', 'Scoreboard-Datei nicht gefunden, starte leer.');
        return {};
    }
}

async function saveScoreboard(scoreboard) {
    try {
        await fs.writeFile(SCOREBOARD_FILE, JSON.stringify(scoreboard, null, 2));
        log('SUCCESS', `Scoreboard gespeichert (${Object.keys(scoreboard).length} Eintraege)`);
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
    res.setHeader('Access-Control-Allow-Methods', '*');
    res.setHeader('Access-Control-Allow-Headers', '*');
    res.setHeader('Access-Control-Allow-Credentials', 'true');
    res.setHeader('Access-Control-Max-Age', '86400');
    res.setHeader('Content-Type', 'application/json');

    if (req.method === 'OPTIONS') {
        log('INFO', 'Preflight OPTIONS beantwortet');
        res.statusCode = 204;
        res.end();
        return;
    }

    if (req.method === 'POST' && req.url === '/score') {
        try {
            const body = await parseBody(req);
            const { id, name, score } = body;

            log('INFO', 'Score-Update empfangen:', { id, name, score });

            if (!id || !name || score === undefined) {
                log('WARN', 'Ungueltige Daten – id, name oder score fehlt', body);
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

    else if (req.method === 'GET' && req.url === '/scoreboard') {
        try {
            const scoreboard = await loadScoreboard();
            const result = Object.entries(scoreboard).map(([id, data]) => ({
                id, name: data.name, score: data.score
            }));

            result.sort((a, b) => b.score - a.score);

            log('SUCCESS', `Scoreboard abgerufen – ${result.length} Eintraege gesendet`);
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
    log('SUCCESS', `Scoreboard Server laeuft auf http://localhost:${PORT}`);
    log('INFO', 'Endpoints: POST /score | GET /scoreboard');
});
"""
# ─────────────────────────────────────────────────────────────────────────────


def get_local_ip():
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip = s.getsockname()[0]
        s.close()
        return ip
    except Exception:
        return "127.0.0.1"


class UnityHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "*")
        self.send_header("Access-Control-Allow-Headers", "*")
        self.send_header("Access-Control-Allow-Credentials", "true")
        self.send_header("Access-Control-Max-Age", "86400")
        path_str = self.path.split("?")[0]
        if path_str.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
            if   path_str.endswith(".js.gz"):   self.send_header("Content-Type", "application/javascript")
            elif path_str.endswith(".wasm.gz"): self.send_header("Content-Type", "application/wasm")
            elif path_str.endswith(".data.gz"): self.send_header("Content-Type", "application/octet-stream")
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        super().end_headers()

    def do_OPTIONS(self):
        self.send_response(204)
        self.end_headers()

    def log_message(self, format, *args):
        pass


def start_python_server():
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    http.server.HTTPServer(("", PORT), UnityHandler).serve_forever()


def start_node_server(script_dir):
    """Startet den eingebetteten Node.js-Server via 'node -e <code>'."""
    try:
        process = subprocess.Popen(
            ["node", "-e", NODE_SERVER_CODE],
            cwd=script_dir,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        return process, None
    except FileNotFoundError:
        return None, "Node.js nicht gefunden.\nBitte Node.js installieren."
    except Exception as e:
        return None, f"Fehler beim Starten:\n{e}"


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    ip  = get_local_ip()
    url = f"http://{ip}:{PORT}"

    # Python HTTP-Server starten
    threading.Thread(target=start_python_server, daemon=True).start()

    # Eingebetteten Node.js-Server starten
    node_process, node_error = start_node_server(script_dir)

    # --- UI ---
    root = tk.Tk()
    root.title("Unity WebGL Server")
    root.resizable(False, False)
    root.attributes("-topmost", True)

    bg, fg, accent = "#1e1e2e", "#cdd6f4", "#89b4fa"
    warn = "#f9e2af"
    root.configure(bg=bg)

    tk.Label(root, text="Unity WebGL Server", font=("Helvetica", 14, "bold"),
             bg=bg, fg=fg).pack(padx=24, pady=(16, 4))

    tk.Label(root, text="Server läuft unter:", font=("Helvetica", 10),
             bg=bg, fg=fg).pack(padx=24, pady=(6, 0))
    tk.Label(root, text=url, font=("Courier", 13, "bold"),
             bg=bg, fg=accent).pack(padx=24, pady=(2, 12))

    # Node.js Status
    tk.Frame(root, bg="#313244", height=1).pack(fill="x", padx=24, pady=(4, 8))

    node_status_frame = tk.Frame(root, bg=bg)
    node_status_frame.pack(padx=24, fill="x", pady=(0, 4))

    tk.Label(node_status_frame, text="Node.js (eingebettet):", font=("Helvetica", 10, "bold"),
             bg=bg, fg=fg).pack(side="left")

    if node_process:
        node_status_text = "✔ läuft"
        node_status_color = "#a6e3a1"
    else:
        node_status_text = "✘ Fehler"
        node_status_color = "#f38ba8"

    tk.Label(node_status_frame, text=node_status_text, font=("Helvetica", 10),
             bg=bg, fg=node_status_color).pack(side="left", padx=(8, 0))

    if node_error:
        tk.Label(root, text=node_error, font=("Helvetica", 9), justify="left",
                 bg=bg, fg=warn).pack(padx=24, anchor="w", pady=(0, 4))

    # Separator
    tk.Frame(root, bg="#313244", height=1).pack(fill="x", padx=24, pady=(4, 10))

    tk.Label(root, text="Anleitung", font=("Helvetica", 10, "bold"),
             bg=bg, fg=fg).pack(padx=24, anchor="w")

    steps = [
        "1.  Mit dem Handy im selben WLAN-Netzwerk sein.",
        "2.  Die oben angezeigte Adresse im Browser öffnen.",
        "3.  Bei einem Development Build kann der\n     erste Ladevorgang 1–2 Minuten dauern.",
    ]
    for step in steps:
        tk.Label(root, text=step, font=("Helvetica", 9), justify="left",
                 bg=bg, fg="#a6adc8").pack(padx=24, pady=(2, 0), anchor="w")

    tk.Frame(root, bg="#313244", height=1).pack(fill="x", padx=24, pady=(10, 6))

    def copy_url():
        root.clipboard_clear()
        root.clipboard_append(url)
        copy_btn.config(text="Kopiert!")
        root.after(2000, lambda: copy_btn.config(text="URL kopieren"))

    copy_btn = tk.Button(root, text="URL kopieren", command=copy_url,
                         bg="#313244", fg=fg, activebackground=accent,
                         activeforeground=bg, relief="flat",
                         font=("Helvetica", 10), padx=12, pady=6)
    copy_btn.pack(pady=(0, 6))

    def on_close():
        if node_process:
            node_process.terminate()
            try:
                node_process.wait(timeout=3)
            except subprocess.TimeoutExpired:
                node_process.kill()
        root.destroy()

    tk.Button(root, text="Server beenden", command=on_close,
              bg="#f38ba8", fg=bg, activebackground="#eba0ac",
              activeforeground=bg, relief="flat",
              font=("Helvetica", 10), padx=12, pady=6).pack(pady=(0, 20))

    root.protocol("WM_DELETE_WINDOW", on_close)

    root.update_idletasks()
    x = (root.winfo_screenwidth()  // 2) - (root.winfo_width()  // 2)
    y = (root.winfo_screenheight() // 2) - (root.winfo_height() // 2)
    root.geometry(f"+{x}+{y}")
    root.mainloop()


if __name__ == "__main__":
    main()