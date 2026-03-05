const http = require("http");
const fs = require("fs");

const BASE_URL = "http://localhost:5000/user/search?first_name=%D0%90%D0%BB&last_name=%D0%98%D0%B2";
const DURATION_MS = 10_000;

function makeRequest() {
  return new Promise((resolve) => {
    const start = performance.now();
    const req = http.get(BASE_URL, { timeout: 30000 }, (res) => {
      let data = "";
      res.on("data", (chunk) => (data += chunk));
      res.on("end", () => resolve({ elapsed: performance.now() - start, status: res.statusCode }));
    });
    req.on("error", () => resolve({ elapsed: performance.now() - start, status: 0 }));
    req.on("timeout", () => { req.destroy(); resolve({ elapsed: performance.now() - start, status: 0 }); });
  });
}

async function runWorker(results, stopTime) {
  while (performance.now() < stopTime) {
    const r = await makeRequest();
    results.push(r);
  }
}

async function runLoadTest(concurrency) {
  const results = [];
  const stopTime = performance.now() + DURATION_MS;
  const workers = [];
  for (let i = 0; i < concurrency; i++) workers.push(runWorker(results, stopTime));
  await Promise.all(workers);

  const latencies = results.map((r) => r.elapsed).sort((a, b) => a - b);
  const errors = results.filter((r) => r.status !== 200).length;
  const sum = latencies.reduce((a, b) => a + b, 0);

  return {
    concurrency,
    total_requests: latencies.length,
    duration_sec: DURATION_MS / 1000,
    throughput_rps: +(latencies.length / (DURATION_MS / 1000)).toFixed(1),
    avg_ms: +(sum / latencies.length).toFixed(1),
    median_ms: +latencies[Math.floor(latencies.length * 0.5)].toFixed(1),
    p95_ms: +latencies[Math.floor(latencies.length * 0.95)].toFixed(1),
    p99_ms: +latencies[Math.floor(latencies.length * 0.99)].toFixed(1),
    max_ms: +latencies[latencies.length - 1].toFixed(1),
    min_ms: +latencies[0].toFixed(1),
    errors,
  };
}

async function main() {
  const label = process.argv[2] || "test";
  const levels = [1, 10, 100, 1000];
  const allResults = [];

  for (const c of levels) {
    process.stdout.write(`Running concurrency=${c} for ${DURATION_MS / 1000}s... `);
    const r = await runLoadTest(c);
    allResults.push(r);
    console.log(`Reqs: ${r.total_requests}, RPS: ${r.throughput_rps}, Avg: ${r.avg_ms}ms, P95: ${r.p95_ms}ms, P99: ${r.p99_ms}ms, Errors: ${r.errors}`);
  }

  const file = `load_test_${label}.json`;
  fs.writeFileSync(file, JSON.stringify(allResults, null, 2));
  console.log(`\nResults saved to ${file}`);
}

main().catch(console.error);
