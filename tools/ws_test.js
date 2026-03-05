const http = require('http');

const ALICE_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImQwZjhhMTU2LWRmOTUtNGI1Mi1iNzIxLTljNDM3OTA2MDAzMCIsImV4cCI6MTc3MjgzOTYwMSwiaXNzIjoiU29jaWFsTmV0d29yayIsImF1ZCI6IlNvY2lhbE5ldHdvcmsifQ.S5VrvxWhu2tdjLZHIZg8vLRc7VCTtTB84oEoNFdOTPA';
const BOB_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImY2MGRiMDIzLWI1OTEtNDhkZS1hY2NmLThjMjhhOGUyZWM3MiIsImV4cCI6MTc3MjgzOTYwMSwiaXNzIjoiU29jaWFsTmV0d29yayIsImF1ZCI6IlNvY2lhbE5ldHdvcmsifQ.MbDNogCTwMO0xu9e7e9DfPVqCumEPn7J7b3tNvpVOFc';

const PORT = 5168;

async function test() {
    // Connect Bob's WebSocket
    console.log('1. Connecting Bob to WebSocket...');

    const WebSocket = require('ws');
    const ws = new WebSocket(`ws://localhost:${PORT}/post/feed/posted?token=${BOB_TOKEN}`);

    const wsReady = new Promise((resolve, reject) => {
        ws.on('open', () => { console.log('   Bob connected to WebSocket!'); resolve(); });
        ws.on('error', (err) => { console.error('   WebSocket error:', err.message); reject(err); });
    });

    const messageReceived = new Promise((resolve, reject) => {
        const timeout = setTimeout(() => reject(new Error('Timeout: no message received')), 10000);
        ws.on('message', (data) => {
            clearTimeout(timeout);
            const msg = JSON.parse(data.toString());
            console.log('3. Bob received post via WebSocket:', JSON.stringify(msg, null, 2));
            resolve(msg);
        });
    });

    await wsReady;

    // Create post as Alice
    console.log('2. Alice creates a post...');
    const postData = JSON.stringify({ text: 'Hello from Alice! Testing WebSocket delivery.' });

    const postResult = await new Promise((resolve, reject) => {
        const req = http.request({
            hostname: 'localhost',
            port: PORT,
            path: '/post/create',
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${ALICE_TOKEN}`
            }
        }, (res) => {
            let body = '';
            res.on('data', (chunk) => body += chunk);
            res.on('end', () => {
                console.log(`   Post created: ${body} (status: ${res.statusCode})`);
                resolve(body);
            });
        });
        req.on('error', reject);
        req.write(postData);
        req.end();
    });

    // Wait for WebSocket message
    try {
        await messageReceived;
        console.log('\n=== TEST PASSED: Bob received Alice\'s post via WebSocket! ===');
    } catch (err) {
        console.error('\n=== TEST FAILED:', err.message, '===');
    }

    ws.close();
    process.exit(0);
}

test().catch(err => { console.error('Error:', err); process.exit(1); });
