# Deployment

Hosts two Fusion samples as public websites on a shared VM, behind Cloudflare:

- `todoapp.actuallab.net`        → TodoApp sample (`sample_todoapp_ws`)
- `blazor-samples.actuallab.net` → Blazor sample (`sample_blazor_ws`)

## Topology

```
Browser ─HTTPS─> Cloudflare (proxied) ─HTTPS─> edge Caddy :443 ─HTTP─> app :8080
```

The **edge Caddy** is the one from the BoardGames deployment on the same VM: it
owns ports 80/443, terminates TLS with a Cloudflare wildcard Origin cert
(`*.actuallab.net`), and reverse-proxies each subdomain to the matching
container. The containers here publish no ports and join that Caddy's network
(`boardgames_default`, referenced as the external `edge` network). Both samples
use Sqlite, so there's no database service.

## First-time host setup

```bash
git clone https://github.com/ActualLab/Fusion.Samples /opt/apps/fusion-samples
cd /opt/apps/fusion-samples/deploy
docker compose -f docker-compose.prod.yml up -d --build
```

Add the two subdomains to the edge Caddyfile (already done in the BoardGames
repo's `deploy/Caddyfile`) and to Cloudflare DNS (proxied A records → VM IP).

## Auto-deploy on push

A systemd timer polls `origin/master` every minute and rebuilds + restarts when
it moves. No GitHub secrets or inbound webhooks.

```bash
sudo cp systemd/fusion-samples-deploy.* /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now fusion-samples-deploy.timer
```

Force a deploy immediately: `deploy/deploy.sh --force`.

## Notes

- OAuth sign-in needs the samples' OAuth apps to whitelist the deployed
  callback URLs; browsing/gameplay works without it.
- Sqlite data is in-container and resets on redeploy (fine for a demo).
