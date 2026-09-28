"""Local WebGL smoke server. --mock-sdk is for QA only, never included in the ZIP."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--mock-sdk', action='store_true')
parser.add_argument('--port', type=int, default=8765)
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]

class Preview(SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path.split('?')[0] == '/sdk.js' and args.mock_sdk:
            data = (root / 'Tools/Yandex/mock-sdk.js').read_bytes()
            self.send_response(200)
            self.send_header('Content-Type', 'application/javascript')
            self.send_header('Content-Length', str(len(data)))
            self.end_headers()
            self.wfile.write(data)
            return
        super().do_GET()

print(f'QA preview http://127.0.0.1:{args.port} mock_sdk={args.mock_sdk}', flush=True)
ThreadingHTTPServer(('127.0.0.1', args.port), partial(Preview, directory=str(root / 'Builds/Yandex'))).serve_forever()
