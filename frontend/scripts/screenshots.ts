/// <reference lib="dom" />
/**
 * Snimke zaslona (docs/screenshots/non-mockup/), podloga za mockupe u
 * READMEu. Pokreće se kad se sučelje promijeni, uz pokrenut backend i Vite
 * dev server (docs/development.md);
 * prvi put treba i `npx playwright install chromium`:
 *
 *   TERMINUS_EMAIL=... TERMINUS_PASSWORD=... npm run screenshots
 *
 * Neobavezno: TERMINUS_URL (zadano http://localhost:5173), SCREENSHOT_STOP
 * (kod stanice, zadano centar) i SCREENSHOT_ROUTE (oznaka linije, zadano 8).
 * Podaci se samo čitaju: GTFS uvoz ide do pregleda, koji ništa ne upisuje.
 * Dijelovi u page.evaluate izvršavaju se u pregledniku (zato lib "dom").
 */
import { mkdir, readFile, stat, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium, type Page } from 'playwright'
import sharp from 'sharp'

const baseUrl = process.env.TERMINUS_URL ?? 'http://localhost:5173'
const email = process.env.TERMINUS_EMAIL
const password = process.env.TERMINUS_PASSWORD
const stopCode = process.env.SCREENSHOT_STOP ?? 'centar'
const routeName = process.env.SCREENSHOT_ROUTE ?? '8'

const outDir = join(dirname(fileURLToPath(import.meta.url)), '../../docs/screenshots/non-mockup')
const maxBytes = 500 * 1024

if (!email || !password) {
  console.error('Postavi TERMINUS_EMAIL i TERMINUS_PASSWORD (admin za dashboard).')
  process.exit(1)
}

// Gumb TanStack Query devtoolsa (samo u dev načinu) ne ide na snimke.
const hideDevtools = '.tsqd-parent-container { display: none !important; }'

/** Čeka da se podaci učitaju: mreža miruje, a loadera i skeletona nema. */
async function settle(page: Page) {
  await page.addStyleTag({ content: hideDevtools })
  await page.waitForLoadState('networkidle')
  await page.waitForFunction(
    () =>
      document.querySelectorAll('.mantine-Loader-root, .mantine-Skeleton-root[data-visible]')
        .length === 0,
  )
  await page.evaluate(() => document.fonts.ready)
}

/**
 * Instanca MapLibre karte iz stanja React komponente (dev build): traži se
 * hook čija vrijednost (ili ref) ima getCanvas.
 */
function findMap() {
  type Hook = { memoizedState: unknown; next: Hook | null }
  type Fiber = { memoizedState: Hook | null; return: Fiber | null }
  const element = document.querySelector('.maplibregl-map')
  const key = element && Object.keys(element).find((k) => k.startsWith('__reactFiber$'))
  let fiber = key ? ((element as unknown as Record<string, Fiber>)[key] ?? null) : null
  while (fiber) {
    for (let hook = fiber.memoizedState; hook && typeof hook === 'object'; hook = hook.next) {
      const state = hook.memoizedState as { current?: unknown } | null
      const candidate =
        state && typeof state === 'object' && 'current' in state ? state.current : state
      if (candidate && typeof (candidate as { getCanvas?: unknown }).getCanvas === 'function') {
        return candidate
      }
    }
    fiber = fiber.return
  }
  return null
}

/**
 * Čeka da karta nacrta pločice podloge. GeoJSON izvori se ne čekaju: vozila
 * se na karti uživo osvježavaju svaki kadar, pa nikad nisu "učitana".
 */
async function waitForMap(page: Page) {
  await page.waitForFunction(`(() => {
    const map = (${findMap.toString()})()
    if (map === null || map.isMoving()) return false
    const managers = Object.values(map.style.tileManagers ?? map.style.sourceCaches ?? {})
    return managers.length > 0 && managers
      .filter((m) => m.getSource().type !== 'geojson')
      .every((m) => m.loaded())
  })()`)
  await page.waitForTimeout(500)
}

/** Klik na točku karte (zemljopisna širina i dužina). */
async function clickOnMap(page: Page, latitude: number, longitude: number) {
  const point = await page.evaluate(
    `(() => {
      const map = (${findMap.toString()})()
      const { x, y } = map.project([${longitude}, ${latitude}])
      const rect = map.getCanvas().getBoundingClientRect()
      return { x: rect.left + x, y: rect.top + y }
    })()`,
  )
  const { x, y } = point as { x: number; y: number }
  await page.mouse.click(x, y)
}

async function save(name: string, png: Buffer) {
  // Paleta (pngquant u sharpu) smanjuje snimke sučelja bez vidljivog gubitka.
  let quality = 90
  let output = await sharp(png).png({ palette: true, quality, effort: 10 }).toBuffer()
  while (output.length > maxBytes && quality > 40) {
    quality -= 10
    output = await sharp(png).png({ palette: true, quality, effort: 10 }).toBuffer()
  }
  const path = join(outDir, name)
  await writeFile(path, output)
  console.log(`${name}: ${Math.round((await stat(path)).size / 1024)} KB (kvaliteta ${quality})`)
}

const browser = await chromium.launch()
const context = await browser.newContext({
  baseURL: baseUrl,
  viewport: { width: 1440, height: 900 },
  deviceScaleFactor: 2,
  colorScheme: 'light',
  locale: 'hr-HR',
  timezoneId: 'Europe/Sarajevo',
})
await context.addInitScript(() => {
  // Svijetla tema i zadana podloga karte, bez obzira na spremljeni izbor.
  localStorage.setItem('mantine-color-scheme-value', 'light')
  localStorage.removeItem('terminus-map-style')
})
const page = await context.newPage()
await mkdir(outDir, { recursive: true })

const login = await page.request.post('/api/auth/login', { data: { email, password } })
if (!login.ok()) {
  throw new Error(`Prijava nije uspjela (${login.status()}).`)
}

const stopRows = (await (
  await page.request.get('/api/stops/table', { params: { search: stopCode, pageSize: 100 } })
).json()) as {
  items: { stop: { id: string; code: string; latitude: number; longitude: number } }[]
}
const stop = stopRows.items.map((i) => i.stop).find((s) => s.code === stopCode)
if (!stop) {
  throw new Error(`Nema stanice s kodom ${stopCode}.`)
}

const routeRows = (await (
  await page.request.get('/api/routes/table', { params: { search: routeName, pageSize: 100 } })
).json()) as { items: { route: { id: string; shortName: string | null } }[] }
const route = routeRows.items.map((i) => i.route).find((r) => r.shortName === routeName)
if (!route) {
  throw new Error(`Nema linije ${routeName}.`)
}

// Pravi e-mail administratora ne ide na snimke.
await context.route('**/api/auth/me', async (request) => {
  const response = await request.fetch()
  const user = (await response.json()) as Record<string, unknown>
  await request.fulfill({ response, json: { ...user, email: 'admin@terminus.ba' } })
})

// a) Javni zaslon polazaka stanice.
await page.goto(`/board/${stop.code}`)
await settle(page)
await save('departures.png', await page.screenshot())

// b) Karta uživo: stanica, prvi polazak linije (ili bilo koji) i njegova trasa.
await page.goto('/live')
await settle(page)
await waitForMap(page)
await page.evaluate(
  `(${findMap.toString()})().jumpTo({ center: [${stop.longitude}, ${stop.latitude}], zoom: 15 })`,
)
await waitForMap(page)
await clickOnMap(page, stop.latitude, stop.longitude)
await settle(page)
const departureRows = page.locator('button[class*="row"]')
const ofRoute = departureRows.filter({ has: page.getByText(routeName, { exact: true }) })
await ((await ofRoute.count()) > 0 ? ofRoute : departureRows).first().click()
await settle(page)
await waitForMap(page)
await save('map.png', await page.screenshot())

// c) Grafikon voznog reda linije s odabranom vožnjom.
await page.goto(`/routes/${route.id}/schedule`)
await settle(page)
await page.keyboard.press('ArrowDown')
await settle(page)
await save('schedule.png', await page.screenshot())

// d) Prva stranica PDF voznog reda stanice, iscrtana s pdf.js.
const pdf = await page.request.post('/api/stops/pdf', {
  data: { ids: [stop.id], timetable: true, routes: false },
})
if (!pdf.ok()) {
  throw new Error(`PDF nije napravljen (${pdf.status()}).`)
}
const pdfjs = dirname(createRequire(import.meta.url).resolve('pdfjs-dist/package.json'))
// pdf.js i stranica s istog (izmišljenog) origina, da worker smije raditi.
const pdfPage = await context.newPage()
await pdfPage.route('http://pdf.local/**', async (request) => {
  const name = new URL(request.request().url()).pathname.slice(1)
  await request.fulfill(
    name
      ? { contentType: 'text/javascript', body: await readFile(join(pdfjs, 'build', name)) }
      : { contentType: 'text/html', body: '<!doctype html>' },
  )
})
await pdfPage.goto('http://pdf.local/')
const pagePng = await pdfPage.evaluate(
  async (data: number[]) => {
    const pdfjsUrl: string = 'http://pdf.local/pdf.min.mjs'
    const lib = await import(pdfjsUrl)
    lib.GlobalWorkerOptions.workerSrc = 'http://pdf.local/pdf.worker.min.mjs'
    const document = await lib.getDocument({ data: new Uint8Array(data) }).promise
    const first = await document.getPage(1)
    // A4 na oko 1240 px širine (150 dpi).
    const viewport = first.getViewport({ scale: 150 / 72 })
    const canvas = Object.assign(window.document.createElement('canvas'), {
      width: Math.round(viewport.width),
      height: Math.round(viewport.height),
    })
    const canvasContext = canvas.getContext('2d')!
    canvasContext.fillStyle = '#fff'
    canvasContext.fillRect(0, 0, canvas.width, canvas.height)
    await first.render({ canvas, canvasContext, viewport }).promise
    return canvas.toDataURL('image/png')
  },
  [...(await pdf.body())],
)
await pdfPage.close()
await save('timetable-pdf.png', Buffer.from(pagePng.split(',')[1], 'base64'))

// e) GTFS: provjera izvoza i pregled uvoza izvezenog feeda (bez upisa).
const feed = await page.request.get('/api/gtfs')
if (!feed.ok()) {
  throw new Error(`GTFS izvoz nije uspio (${feed.status()}).`)
}
await page.goto('/gtfs')
await settle(page)
await page.locator('input[type="file"]').setInputFiles({
  name: 'gtfs.zip',
  mimeType: 'application/zip',
  buffer: await feed.body(),
})
// Korak pregleda; gumb "Uvezi" se ne dira.
await page.getByRole('button', { name: 'Druga datoteka' }).waitFor()
await settle(page)
await save('gtfs.png', await page.screenshot())

await browser.close()
