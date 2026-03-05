const http = require('http');
const PORT = 5168;

let ALICE_ID, BOB_ID, ALICE_TOKEN, BOB_TOKEN;

function httpRequest(method, path, body, token) {
    return new Promise((resolve, reject) => {
        const headers = { 'Content-Type': 'application/json' };
        if (token) headers['Authorization'] = `Bearer ${token}`;
        const req = http.request({ hostname: 'localhost', port: PORT, path, method, headers }, (res) => {
            let data = '';
            res.on('data', c => data += c);
            res.on('end', () => resolve({ status: res.statusCode, body: data }));
        });
        req.on('error', reject);
        if (body) req.write(JSON.stringify(body));
        req.end();
    });
}

async function setup() {
    const r1 = await httpRequest('POST', '/user/register', { firstName: 'LoadAlice', lastName: 'Test', password: 'test123' });
    ALICE_ID = JSON.parse(r1.body).userId;
    const r2 = await httpRequest('POST', '/user/register', { firstName: 'LoadBob', lastName: 'Test', password: 'test123' });
    BOB_ID = JSON.parse(r2.body).userId;

    const t1 = await httpRequest('POST', '/login', { id: ALICE_ID, password: 'test123' });
    ALICE_TOKEN = JSON.parse(t1.body).token;
    const t2 = await httpRequest('POST', '/login', { id: BOB_ID, password: 'test123' });
    BOB_TOKEN = JSON.parse(t2.body).token;

    console.log(`Alice: ${ALICE_ID}, Bob: ${BOB_ID}`);
}

async function runBench(concurrency, durationSec) {
    const end = Date.now() + durationSec * 1000;
    let totalSend = 0, totalList = 0, sendErrors = 0, listErrors = 0;
    const sendLatencies = [], listLatencies = [];

    const workers = Array.from({ length: concurrency }, async () => {
        while (Date.now() < end) {
            // Send
            const s1 = Date.now();
            try {
                const r = await httpRequest('POST', `/dialog/${BOB_ID}/send`, { text: 'Bench msg' }, ALICE_TOKEN);
                if (r.status === 200) { totalSend++; sendLatencies.push(Date.now() - s1); }
                else sendErrors++;
            } catch { sendErrors++; }

            // List
            const s2 = Date.now();
            try {
                const r = await httpRequest('GET', `/dialog/${BOB_ID}/list`, null, ALICE_TOKEN);
                if (r.status === 200) { totalList++; listLatencies.push(Date.now() - s2); }
                else listErrors++;
            } catch { listErrors++; }
        }
    });

    await Promise.all(workers);

    const sortedSend = sendLatencies.sort((a, b) => a - b);
    const sortedList = listLatencies.sort((a, b) => a - b);
    const p = (arr, pct) => arr[Math.floor(arr.length * pct / 100)] || 0;
    const avg = (arr) => arr.length ? (arr.reduce((a, b) => a + b, 0) / arr.length).toFixed(1) : 0;

    return {
        concurrency,
        send: { total: totalSend, rps: (totalSend / durationSec).toFixed(1), avg: avg(sortedSend), p95: p(sortedSend, 95), p99: p(sortedSend, 99), errors: sendErrors },
        list: { total: totalList, rps: (totalList / durationSec).toFixed(1), avg: avg(sortedList), p95: p(sortedList, 95), p99: p(sortedList, 99), errors: listErrors }
    };
}

async function main() {
    const label = process.argv[2] || 'dialog_bench';
    await setup();

    const results = [];
    for (const c of [1, 10, 100]) {
        process.stdout.write(`Running c=${c} for 10s... `);
        const r = await runBench(c, 10);
        console.log(`Send: ${r.send.rps} RPS, List: ${r.list.rps} RPS`);
        results.push(r);
    }

    require('fs').writeFileSync(`dialog_bench_${label}.json`, JSON.stringify(results, null, 2));
    console.log(`\nResults saved to dialog_bench_${label}.json`);
}

main().catch(console.error);
