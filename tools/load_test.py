import http.client
import time
import json
import sys
import statistics
import concurrent.futures
import urllib.parse

BASE_HOST = "localhost"
BASE_PORT = 5000
SEARCH_PATH = "/user/search?first_name=Ал&last_name=Ив"
DURATION_SECONDS = 10

def make_request():
    start = time.perf_counter()
    try:
        conn = http.client.HTTPConnection(BASE_HOST, BASE_PORT, timeout=30)
        conn.request("GET", SEARCH_PATH)
        resp = conn.getresponse()
        resp.read()
        status = resp.status
        conn.close()
        elapsed = time.perf_counter() - start
        return elapsed, status
    except Exception as e:
        elapsed = time.perf_counter() - start
        return elapsed, 0

def run_load_test(concurrency, duration=DURATION_SECONDS):
    results = []
    errors = 0
    stop_time = time.perf_counter() + duration
    total_requests = 0

    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as executor:
        futures = []
        while time.perf_counter() < stop_time:
            if len(futures) < concurrency * 2:
                futures.append(executor.submit(make_request))
            
            done = [f for f in futures if f.done()]
            for f in done:
                elapsed, status = f.result()
                results.append(elapsed)
                if status != 200:
                    errors += 1
                total_requests += 1
                futures.remove(f)

        for f in concurrent.futures.as_completed(futures, timeout=30):
            elapsed, status = f.result()
            results.append(elapsed)
            if status != 200:
                errors += 1
            total_requests += 1

    if not results:
        return {}

    results_ms = [r * 1000 for r in results]
    results_ms.sort()

    return {
        "concurrency": concurrency,
        "total_requests": len(results_ms),
        "duration_sec": duration,
        "throughput_rps": len(results_ms) / duration,
        "avg_ms": statistics.mean(results_ms),
        "median_ms": statistics.median(results_ms),
        "p95_ms": results_ms[int(len(results_ms) * 0.95)] if len(results_ms) > 20 else max(results_ms),
        "p99_ms": results_ms[int(len(results_ms) * 0.99)] if len(results_ms) > 100 else max(results_ms),
        "max_ms": max(results_ms),
        "min_ms": min(results_ms),
        "errors": errors
    }

if __name__ == "__main__":
    label = sys.argv[1] if len(sys.argv) > 1 else "test"
    concurrency_levels = [1, 10, 100, 1000]
    all_results = []

    for c in concurrency_levels:
        print(f"Running with concurrency={c} for {DURATION_SECONDS}s...")
        result = run_load_test(c)
        all_results.append(result)
        print(f"  Requests: {result['total_requests']}, "
              f"RPS: {result['throughput_rps']:.1f}, "
              f"Avg: {result['avg_ms']:.1f}ms, "
              f"P95: {result['p95_ms']:.1f}ms, "
              f"P99: {result['p99_ms']:.1f}ms, "
              f"Errors: {result['errors']}")

    output_file = f"load_test_{label}.json"
    with open(output_file, "w") as f:
        json.dump(all_results, f, indent=2)
    print(f"\nResults saved to {output_file}")
