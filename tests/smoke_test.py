"""
End-to-end smoke test for the Smart Solar Microgrid API.

Run against a THROWAWAY database (it creates data):
  MongoDb__ConnectionString=mongodb://127.0.0.1:27555 MongoDb__DatabaseName=SmokeTest dotnet run
  BASE=http://localhost:5055 python3 tests/smoke_test.py

It reads /swagger/v1/swagger.json and fails if any API route was not exercised, if the API ever returns a
status code the spec does not declare, or if a response body does not match its declared schema.
"""
import json, os, re, sys, urllib.request, urllib.error, datetime as dt

BASE = os.environ.get("BASE", "http://localhost:5055")
hits, results, observed = set(), [], []

def call(method, path, body=None, tok=None):
    req = urllib.request.Request(BASE + path, method=method, data=json.dumps(body).encode() if body is not None else None)
    req.add_header("Content-Type", "application/json")
    if tok: req.add_header("Authorization", "Bearer " + tok)
    hits.add((method, re.sub(r"\?.*", "", path)))
    try:
        r = urllib.request.urlopen(req); text = r.read().decode(); code = r.status
    except urllib.error.HTTPError as e:
        text = e.read().decode(); code = e.code
    try: parsed = json.loads(text) if text else None
    except ValueError: parsed = text
    observed.append((method, re.sub(r"\?.*", "", path), code, parsed))
    return code, parsed

def check(name, got, expected):
    ok = got == expected
    results.append(ok)
    print(("PASS " if ok else "FAIL ") + name + ("" if ok else f"  (got {got!r}, expected {expected!r})"))

EMAIL = {"backoffice": "backoffice@smartsolar.lk", "operator": "operator@smartsolar.lk", "199012345678": "prosumer@example.com",
         "199256789012": "nimal@example.com", "200011112222": "a@b.co", "200033334444": "c@d.co", "op2": "op2@smartsolar.lk"}
def login(u, p="Password123!"): return call("POST", "/api/auth/login", {"email": EMAIL.get(u, u), "password": p})
def iso(h): return (dt.datetime.now(dt.timezone.utc) + dt.timedelta(hours=h)).strftime("%Y-%m-%dT%H:%M:%SZ")
def slot_body(sid, start_h, kwh=50): return {"stationId": sid, "startTime": iso(start_h), "endTime": iso(start_h + 2), "availableKwh": kwh, "pricePerKwh": 10}

check("health", call("GET", "/health"), (200, "Healthy"))
bo = login("backoffice")[1]["token"]; op = login("operator")[1]["token"]; pr = login("199012345678")[1]["token"]
check("bad password rejected", login("backoffice", "nope")[0], 400)
check("no token -> 401", call("GET", "/api/users")[0], 401)

# ---- seed data present ----
s, stations = call("GET", "/api/stations"); check("seed: 3 stations", len(stations), 3)
check("seed: all four collections reachable", (len(call("GET", "/api/slots")[1]) >= 6, len(call("GET", "/api/users", tok=bo)[1]) >= 5), (True, True))
check("seed: pending prosumer present", [p["nic"] for p in call("GET", "/api/prosumers?status=pending", tok=bo)[1]], ["198834567890"])
sid = next(x["id"] for x in stations if x["name"] == "Colombo Solar Hub")

# ---- users (Backoffice) ----
check("users: list", call("GET", "/api/users", tok=bo)[0], 200)
s, u = call("POST", "/api/users", {"username": "op2", "email": "op2@smartsolar.lk", "password": "secret1", "role": "GridOperator"}, bo); check("users: create operator", s, 201)
check("users: Prosumer role rejected", call("POST", "/api/users", {"username": "pp", "email": "pp@smartsolar.lk", "password": "secret1", "role": "Prosumer"}, bo)[0], 400)
check("users: duplicate rejected", call("POST", "/api/users", {"username": "op2", "email": "op2@smartsolar.lk", "password": "secret1", "role": "GridOperator"}, bo)[0], 400)
check("users: update (disable)", call("PUT", f"/api/users/{u['id']}", {"role": "GridOperator", "isActive": False}, bo)[1]["isActive"], False)
check("users: disabled user cannot login", login("op2", "secret1")[0], 400)
check("users: operator forbidden", call("GET", "/api/users", tok=op)[0], 403)

# ---- prosumers ----
check("prosumer: register", call("POST", "/api/prosumers/register", {"nic": "200011112222", "fullName": "New P", "email": "a@b.co", "phone": "0771", "password": "secret1"})[0], 200)
check("prosumer: duplicate NIC rejected", call("POST", "/api/prosumers/register", {"nic": "200011112222", "fullName": "x", "email": "a@b.co", "phone": "1", "password": "secret1"})[0], 400)
check("prosumer: invalid email rejected", call("POST", "/api/prosumers/register", {"nic": "1", "fullName": "x", "email": "bad", "phone": "1", "password": "secret1"})[0], 400)
check("prosumer: pending cannot login", "not active" in login("200011112222", "secret1")[1]["error"], True)
check("prosumer: staff lookup", call("GET", "/api/prosumers/200011112222", tok=op)[0], 200)
check("prosumer: operator cannot list", call("GET", "/api/prosumers", tok=op)[0], 403)
check("prosumer: list all", len(call("GET", "/api/prosumers", tok=bo)[1]) >= 4, True)
check("prosumer: activate pending", call("POST", "/api/prosumers/200011112222/activate", tok=bo)[1]["isActive"], True)
check("prosumer: login after activation", login("200011112222", "secret1")[0], 200)
check("prosumer: backoffice update", call("PUT", "/api/prosumers/200011112222", {"fullName": "Renamed", "email": "a@b.co", "phone": "1", "address": "x"}, bo)[1]["fullName"], "Renamed")
check("prosumer: backoffice create", call("POST", "/api/prosumers", {"nic": "200033334444", "fullName": "C", "email": "c@d.co", "phone": "1", "password": "secret1"}, bo)[0], 201)
check("prosumer: me", call("GET", "/api/prosumers/me", tok=pr)[1]["nic"], "199012345678")
check("prosumer: update me", call("PUT", "/api/prosumers/me", {"fullName": "Sample Prosumer", "email": "p@e.co", "phone": "077", "address": "Colombo 3"}, pr)[1]["address"], "Colombo 3")
check("prosumer: deactivation request", call("POST", "/api/prosumers/me/deactivation-request", tok=pr)[0], 204)
check("prosumer: shows in deactivation-requested", "199012345678" in [p["nic"] for p in call("GET", "/api/prosumers?status=deactivation-requested", tok=bo)[1]], True)
check("prosumer: deactivate blocked (open reservations)", call("POST", "/api/prosumers/199012345678/deactivate", tok=bo)[0], 400)
check("prosumer: deactivate", call("POST", "/api/prosumers/200033334444/deactivate", tok=bo)[1]["isActive"], False)
check("prosumer: deactivated cannot login", login("200033334444", "secret1")[0], 400)
check("prosumer: reactivate", call("POST", "/api/prosumers/200033334444/activate", tok=bo)[1]["isActive"], True)
check("prosumer: bad status filter", call("GET", "/api/prosumers?status=zzz", tok=bo)[0], 400)
check("prosumer: unknown NIC 404", call("GET", "/api/prosumers/000", tok=bo)[0], 404)

# ---- stations ----
new = {"name": "Test Hub", "location": "Jaffna", "latitude": 9.66, "longitude": 80.02, "capacityKwh": 100, "batteryStorageSlots": 4}
s, t = call("POST", "/api/stations", new, bo); check("station: create", s, 201)
check("station: operator cannot create", call("POST", "/api/stations", new, op)[0], 403)
check("station: invalid GPS rejected", call("POST", "/api/stations", {**new, "latitude": 999}, bo)[0], 400)
new["name"] = "Test Hub 2"; check("station: update", call("PUT", f"/api/stations/{t['id']}", new, bo)[1]["name"], "Test Hub 2")
check("station: operator battery slots", call("PUT", f"/api/stations/{t['id']}/battery-slots", {"availableBatterySlots": 1}, op)[1]["availableBatterySlots"], 1)
check("station: battery slots > total rejected", call("PUT", f"/api/stations/{t['id']}/battery-slots", {"availableBatterySlots": 99}, op)[0], 400)
s, near = call("GET", "/api/stations/nearby?latitude=6.93&longitude=79.85&radiusKm=10"); check("station: nearby finds Colombo only", [n["station"]["name"] for n in near], ["Colombo Solar Hub"])
check("station: nearby sorted by distance", [n["station"]["name"] for n in call("GET", "/api/stations/nearby?latitude=7.0&longitude=80.0&radiusKm=500")[1]][0] in ("Colombo Solar Hub", "Kandy Hill Microgrid"), True)
check("station: nearby bad coords", call("GET", "/api/stations/nearby?latitude=200&longitude=0")[0], 400)
check("station: deactivate", call("DELETE", f"/api/stations/{t['id']}", tok=bo)[0], 204)
check("station: hidden from active list", t["id"] in [x["id"] for x in call("GET", "/api/stations")[1]], False)
check("station: reactivate", call("POST", f"/api/stations/{t['id']}/activate", tok=bo)[1]["isActive"], True)

# ---- slots ----
s, far = call("POST", "/api/slots", slot_body(sid, 72), bo); check("slot: create", s, 200)
s, soon = call("POST", "/api/slots", slot_body(sid, 5), bo)
s, out = call("POST", "/api/slots", slot_body(sid, 240), bo)
check("slot: operator forbidden", call("POST", "/api/slots", slot_body(sid, 30), op)[0], 403)
check("slot: end before start rejected", call("POST", "/api/slots", {**slot_body(sid, 30), "endTime": iso(29)}, bo)[0], 400)
check("slot: unknown station rejected", call("POST", "/api/slots", {**slot_body("000000000000000000000000", 30)}, bo)[0], 400)
check("slot: list by station", len(call("GET", f"/api/slots?stationId={sid}")[1]) >= 3, True)
tmp = call("POST", "/api/slots", slot_body(t["id"], 50), bo)[1]
check("slot: update", call("PUT", f"/api/slots/{tmp['id']}", slot_body(t["id"], 60, 70), bo)[1]["availableKwh"], 70)
check("slot: delete", call("DELETE", f"/api/slots/{tmp['id']}", tok=bo)[0], 204)
check("slot: delete missing 404", call("DELETE", f"/api/slots/{tmp['id']}", tok=bo)[0], 404)

# ---- prosumer reservations ----
def kwh(slot): return next(x for x in call("GET", "/api/slots")[1] if x["id"] == slot["id"])["availableKwh"]
s, r1 = call("POST", "/api/reservations", {"slotId": far["id"], "energyKwh": 20}, pr); check("resv: create is Pending, no QR yet", (s, r1["status"], r1["qrToken"]), (200, "Pending", None))
check("resv: summary has station + price", (r1["stationName"], r1["totalPrice"]), ("Colombo Solar Hub", 200))
check("resv: beyond 7 days rejected", call("POST", "/api/reservations", {"slotId": out["id"], "energyKwh": 5}, pr)[0], 400)
check("resv: insufficient energy rejected", call("POST", "/api/reservations", {"slotId": far["id"], "energyKwh": 31}, pr)[0], 400)
check("resv: slot energy reduced", kwh(far), 30)
check("resv: update", call("PUT", f"/api/reservations/{r1['id']}", {"energyKwh": 25}, pr)[1]["energyKwh"], 25)
check("resv: slot energy adjusted", kwh(far), 25)
s, r2 = call("POST", "/api/reservations", {"slotId": soon["id"], "energyKwh": 5}, pr)
check("resv: 12h rule blocks update", call("PUT", f"/api/reservations/{r2['id']}", {"energyKwh": 6}, pr)[0], 400)
check("resv: 12h rule blocks cancel", call("DELETE", f"/api/reservations/{r2['id']}", tok=pr)[0], 400)
check("resv: other prosumer cannot touch it", call("PUT", f"/api/reservations/{r1['id']}", {"energyKwh": 1}, login("199256789012")[1]["token"])[0], 404)
s, r5 = call("POST", "/api/reservations", {"slotId": far["id"], "energyKwh": 5}, pr)
check("resv: cancel returns energy", (call("DELETE", f"/api/reservations/{r5['id']}", tok=pr)[1]["status"], kwh(far)), ("Cancelled", 25))
check("resv: cannot edit cancelled", call("PUT", f"/api/reservations/{r5['id']}", {"energyKwh": 1}, pr)[0], 400)
check("resv: prosumer cannot approve", call("POST", f"/api/staff/reservations/{r1['id']}/approve", tok=pr)[0], 403)
check("resv: unapproved QR cannot be verified", call("POST", "/api/operator/verify-qr", {"qrToken": "nope"}, op)[0], 404)

# ---- dashboard + views ----
s, d = call("GET", "/api/dashboard", tok=pr); check("dash: prosumer own pending", d["pendingReservations"], 2)
s, a = call("POST", f"/api/staff/reservations/{r1['id']}/approve", tok=op); check("resv: approve releases QR", (s, a["status"], bool(a["qrToken"])), (200, "Confirmed", True))
check("resv: approve twice rejected", call("POST", f"/api/staff/reservations/{r1['id']}/approve", tok=op)[0], 400)
s, d = call("GET", "/api/dashboard", tok=pr); check("dash: approved-future + pending counts", (d["approvedFutureReservations"], d["pendingReservations"]), (2, 1))
check("dash: staff sees system-wide", call("GET", "/api/dashboard", tok=bo)[1]["pendingReservations"] >= 2, True)
check("view: pending", [x["id"] for x in call("GET", "/api/reservations/pending", tok=pr)[1]], [r2["id"]])
check("view: current", {x["id"] for x in call("GET", "/api/reservations/current", tok=pr)[1]} >= {r1["id"], r2["id"]}, True)
hist = [x["status"] for x in call("GET", "/api/reservations/history", tok=pr)[1]]
check("view: history has completed + cancelled, no open", ("Completed" in hist, "Cancelled" in hist, "Pending" in hist), (True, True, False))
check("filter: state", {x["status"] for x in call("GET", "/api/reservations?state=Completed", tok=pr)[1]}, {"Completed"})
check("filter: search by station", len(call("GET", "/api/reservations?search=colombo", tok=pr)[1]) >= 3, True)
check("filter: search no match", call("GET", "/api/reservations?search=zzzz", tok=pr)[1], [])
# the 72h slot holds r1 (approved) and r5 (cancelled); the date filter does not filter by status
check("filter: date range", len(call("GET", f"/api/reservations?from={iso(60)}&to={iso(80)}", tok=pr)[1]), 2)
check("filter: bad state", call("GET", "/api/reservations?state=zzz", tok=pr)[0], 400)

# ---- operator QR ----
s, v = call("POST", "/api/operator/verify-qr", {"qrToken": a["qrToken"]}, op); check("qr: verify approved", (s, v["id"]), (200, r1["id"]))
check("station: deactivate blocked by open reservation", call("DELETE", f"/api/stations/{sid}", tok=bo)[0], 400)
check("slot: delete blocked by open reservation", call("DELETE", f"/api/slots/{far['id']}", tok=bo)[0], 400)
check("qr: prosumer cannot finalize", call("POST", f"/api/operator/reservations/{r1['id']}/finalize", tok=pr)[0], 403)
check("qr: pending cannot be finalized", call("POST", f"/api/operator/reservations/{r2['id']}/finalize", tok=op)[0], 400)
check("qr: finalize", call("POST", f"/api/operator/reservations/{r1['id']}/finalize", tok=op)[1]["status"], "Completed")
check("qr: finalize twice rejected", call("POST", f"/api/operator/reservations/{r1['id']}/finalize", tok=op)[0], 400)
check("qr: completed token no longer verifies", call("POST", "/api/operator/verify-qr", {"qrToken": a["qrToken"]}, op)[0], 400)

# ---- staff reservations ----
check("staff: list all", len(call("GET", "/api/staff/reservations", tok=bo)[1]) >= 6, True)
check("staff: filter by NIC", {x["prosumerNic"] for x in call("GET", "/api/staff/reservations?prosumerNic=199256789012", tok=bo)[1]}, {"199256789012"})
s, r3 = call("POST", "/api/staff/reservations", {"prosumerNic": "200011112222", "slotId": far["id"], "energyKwh": 3}, op); check("staff: book for prosumer", (s, r3["prosumerNic"]), (200, "200011112222"))
check("staff: inactive prosumer rejected", call("POST", "/api/staff/reservations", {"prosumerNic": "198834567890", "slotId": far["id"], "energyKwh": 3}, op)[0], 400)
check("staff: update", call("PUT", f"/api/staff/reservations/{r3['id']}", {"energyKwh": 4}, bo)[1]["energyKwh"], 4)
check("staff: cancel", call("DELETE", f"/api/staff/reservations/{r3['id']}", tok=bo)[1]["status"], "Cancelled")
check("staff: prosumer forbidden", call("GET", "/api/staff/reservations", tok=pr)[0], 403)

# ---- route coverage: every operation in Swagger must have been called ----
spec = call("GET", "/swagger/v1/swagger.json")[1]
missing = []
for path, ops in spec["paths"].items():
    for method in ops:
        pat = re.compile("^" + re.sub(r"\{[^}]+\}", "[^/]+", path) + "$")
        if not any(m == method.upper() and pat.match(h) for m, h in hits): missing.append(f"{method.upper()} {path}")
check("coverage: every Swagger route exercised", missing, [])

# ---- contract: statuses and bodies must match the Swagger spec ----
def template_for(method, path):
    for tpl, ops in spec["paths"].items():
        if method.lower() in ops and re.match("^" + re.sub(r"\{[^}]+\}", "[^/]+", tpl) + "$", path): return tpl
def resolve(sch): return spec["components"]["schemas"][sch["$ref"].split("/")[-1]] if "$ref" in sch else sch
def conforms(body, sch, where):
    sch = resolve(sch)
    if body is None: return sch.get("nullable", False) or "type" not in sch and "properties" not in sch, f"{where} is null"
    t = sch.get("type")
    if t == "array":
        for i, item in enumerate(body):
            ok, why = conforms(item, sch["items"], f"{where}[{i}]")
            if not ok: return False, why
        return isinstance(body, list), where
    if t == "object" or "properties" in sch:
        if not isinstance(body, dict): return False, f"{where} is not an object"
        if set(body) != set(sch.get("properties", {})): return False, f"{where} keys {sorted(body)} != {sorted(sch.get('properties', {}))}"
        for k, v in body.items():
            ok, why = conforms(v, sch["properties"][k], f"{where}.{k}")
            if not ok: return False, why
        return True, where
    py = {"string": str, "integer": int, "number": (int, float), "boolean": bool}.get(t)
    if t == "integer": return isinstance(body, int) and not isinstance(body, bool), f"{where} not integer"
    return (py is None or isinstance(body, py)), f"{where} not {t}"
undeclared, mismatched = [], []
for method, path, code, body in observed:
    if path == "/health" or path.startswith("/swagger"): continue
    tpl = template_for(method, path)
    resp = spec["paths"][tpl][method.lower()]["responses"].get(str(code))
    if resp is None: undeclared.append(f"{method} {tpl} -> {code}"); continue
    schema = resp.get("content", {}).get("application/json", {}).get("schema")
    if schema and body is not None:
        ok, why = conforms(body, schema, f"{method} {tpl} [{code}]")
        if not ok: mismatched.append(why)
check("contract: every returned status is declared in Swagger", sorted(set(undeclared)), [])
check("contract: every response body matches its Swagger schema", sorted(set(mismatched))[:5], [])
print(f"\n{sum(results)}/{len(results)} checks passed")
sys.exit(0 if all(results) else 1)
