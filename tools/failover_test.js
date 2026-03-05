const http = require("http");
const fs = require("fs");

const BASE = "http://localhost:5000";
const DURATION_MS = 10_000;

function makeRegisterRequest(i) {
    return new Promise((resolve) => {
        const start = performance.now();
        const body = JSON.stringify({
            firstName: `Test${i}`,
            lastName: `User${i}`,
            birthDate: "1990-01-01",
            gender: "male",
            interests: "Testing",
            city: "Moscow",
            password: "test123",
        });
        const req = http.request(
            `${BASE}/user/register`,
            { method: "POST", headers: { "Content-Type": "application/json" }, timeout: 10000 },
            (res) => {
                let data = "";
                res.on("data", (chunk) => (data += chunk));
                res.on("end", () => resolve({ elapsed: performance.now() - start, status: res.statusCode, body: data }));
            }
        );
        req.on("error", () => resolve({ elapsed: performance.now() - start, status: 0, body: "" }));
        req.on("timeout", () => { req.destroy(); resolve({ elapsed: performance.now() - start, status: 0, body: "" }); });
        req.write(body);
        req.end();
    });
}

async function main() {
    const concurrency = parseInt(process.argv[2] || "10");
    const duration = parseInt(process.argv[3] || "10") * 1000;

    console.log(`Running write load test: concurrency=${concurrency}, duration=${duration / 1000}s`);

    let sent = 0;
    let success = 0;
    let errors = 0;
    const stopTime = performance.now() + duration;

    async function worker() {
        while (performance.now() < stopTime) {
            const i = ++sent;
            const r = await makeRegisterRequest(i);
            if (r.status === 200) success++;
            else errors++;
        }
    }

    const workers = [];
    for (let i = 0; i < concurrency; i++) workers.push(worker());
    await Promise.all(workers);

    console.log(`\nResults:`);
    console.log(`  Sent:    ${sent}`);
    console.log(`  Success: ${success}`);
    console.log(`  Errors:  ${errors}`);
    console.log(`  RPS:     ${(success / (duration / 1000)).toFixed(1)}`);

    fs.writeFileSync(
        "failover_results.json",
        JSON.stringify({ sent, success, errors, duration_sec: duration / 1000 }, null, 2)
    );
}

main().catch(console.error);
