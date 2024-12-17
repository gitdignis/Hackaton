from flask import Flask, request, send_file, render_template_string
import ast

app = Flask(__name__)

def analisar_codigo(codigo):
    resultado = []
    linhas = codigo.split('\n')
    for numero_linha, linha in enumerate(linhas, start=1):
        if 'eval(' in linha:
            resultado.append(f"{numero_linha}|CRI|Uso da função eval pode ser inseguro|Substituir eval por alternativas mais seguras")
        elif 'exec(' in linha:
            resultado.append(f"{numero_linha}|CRI|Uso da função exec pode ser inseguro|Evitar o uso de exec para prevenir vulnerabilidades")
        elif 'import *' in linha:
            resultado.append(f"{numero_linha}|MAI|Importação global pode levar a conflitos|Importar apenas os módulos necessários")
        elif 'print(' in linha:
            resultado.append(f"{numero_linha}|WRN|Uso da função print encontrado|Utilizar logging em vez de print para produção")
        # Adicionar mais regras de análise conforme necessário
    return resultado

@app.route('/', methods=['GET'])
def index():
    return render_template_string('''
    <h1>Analisador de Código</h1>
    <form action="/analisar" method="post" enctype="multipart/form-data">
        <input type="file" name="ficheiro">
        <input type="submit" value="Analisar Código">
    </form>
    ''')

@app.route('/analisar', methods=['POST'])
def analisar():
    ficheiro = request.files['ficheiro']
    codigo = ficheiro.read().decode('utf-8')
    resultado = analisar_codigo(codigo)
    with open('resultado.txt', 'w', encoding='utf-8') as f:
        for linha in resultado:
            f.write(linha + '\n')
    return send_file('resultado.txt', as_attachment=True)

if __name__ == '__main__':
    app.run()