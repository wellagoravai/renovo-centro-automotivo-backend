import json
import urllib.request

# Login
data = json.dumps({'Username':'admin','Password':'Renovo@123'}).encode('utf-8')
req = urllib.request.Request('http://localhost:5235/api/Auth/login', data=data, headers={'Content-Type':'application/json'})
res = urllib.request.urlopen(req)
login = json.load(res)
token = login['token']
print('TOKEN', token)

# Create service order
body = {
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

req2 = urllib.request.Request(
    'http://localhost:5235/api/service-orders/with-customer-vehicle',
    data=json.dumps(body).encode('utf-8'),
    headers={'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token},
    method='POST'
)

try:
    res2 = urllib.request.urlopen(req2)
    print('STATUS', res2.status)
    print(res2.read().decode('utf-8'))
except urllib.error.HTTPError as e:
    print('ERROR', e.code, e.reason)
    print(e.read().decode('utf-8'))