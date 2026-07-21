import json
import urllib.request
import urllib.error

# Step 1: Login
print("=== STEP 1: LOGIN ===")
data = json.dumps({'Username':'admin','Password':'Renovo@123'}).encode('utf-8')
req = urllib.request.Request('http://localhost:5235/api/Auth/login', data=data, headers={'Content-Type':'application/json'})
res = urllib.request.urlopen(req)
login = json.load(res)
token = login['token']
user = login['user']
print('Login successful')
print('Token:', token[:50] + '...')
print('User permissions:', user.get('Permissions', 'N/A'))

# Step 2: Simulate frontend - store in "localStorage" (just keep in memory)
print("\n=== STEP 2: SIMULATE FRONTEND STORAGE ===")
print("Token stored in localStorage")

# Step 3: Create service order (exactly like frontend does)
print("\n=== STEP 3: CREATE SERVICE ORDER ===")
requestData = {
    'problemReported': 'Teste .NET',
    'notes': 'Teste',
    'estimatedDate': '2026-07-20T10:00:00',
    'status': 'Recebido',
    'responsibleUser': 'Usuario Atual',
    'customer': {
        'name': 'Teste da Silva',
        'document': '148.022.550-98',
        'whatsApp': '+5511999999999',
        'phone': '(11) 99999-9999',
        'email': 'teste@teste.com',
        'address': 'Rua Teste, 100'
    },
    'vehicle': {
        'plate': 'EXV-5683',
        'brand': 'Toyota',
        'model': 'Corolla',
        'year': 2020,
        'color': 'Prata',
        'mileage': 12345,
        'fuel': 'Flex'
    }
}

# This is exactly what the frontend api.post() does
headers = {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
}

req2 = urllib.request.Request(
    'http://localhost:5235/api/service-orders/with-customer-vehicle',
    data=json.dumps(requestData).encode('utf-8'),
    headers=headers,
    method='POST'
)

try:
    res2 = urllib.request.urlopen(req2)
    print('SUCCESS! Status:', res2.status)
    response_data = json.loads(res2.read().decode('utf-8'))
    print('Service Order ID:', response_data.get('id'))
    print('Service Order Number:', response_data.get('number'))
except urllib.error.HTTPError as e:
    print('ERROR! Status:', e.code)
    print('Response:', e.read().decode('utf-8'))