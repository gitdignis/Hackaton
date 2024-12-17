from flask import Flask, request, render_template_string, send_file, send_from_directory
import os

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
    <!DOCTYPE html>
    <html lang="pt">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Analisador de Código</title>
        <link href="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css" rel="stylesheet">
    </head>
    <body>
        <div class="container mt-5">
            <h1 class="text-center">Analisador de Código</h1>
            <form action="/analisar" method="post" enctype="multipart/form-data" class="mt-4">
                <div class="form-group">
                    <label for="ficheiro">Escolher Ficheiro</label>
                    <input type="file" name="ficheiro" id="ficheiro" class="form-control-file">
                </div>
                <button type="submit" class="btn btn-primary">Analisar Código</button>
            </form>
        </div>
        <script>
            document.getElementById('ficheiro').onchange = function () {
                var fileName = this.value.split('\\\\').pop();
                if (fileName) {
                    this.nextElementSibling.innerText = fileName;
                } else {
                    this.nextElementSibling.innerText = 'Nenhum ficheiro escolhido';
                }
            };
            
            // Alterar o texto do botão de upload de ficheiro para Português
            document.addEventListener('DOMContentLoaded', function() {
                var fileInput = document.getElementById('ficheiro');
                var fileLabel = fileInput.nextElementSibling;
                fileLabel.innerText = 'Escolher Ficheiro';
            });
        </script>
    </body>
    </html>
    ''')

@app.route('/analisar', methods=['POST'])
def analisar():
    ficheiro = request.files['ficheiro']
    nome_ficheiro = ficheiro.filename
    codigo = ficheiro.read().decode('utf-8')
    resultado = analisar_codigo(codigo)
    primeiras_linhas = '\n'.join(resultado[:10]) if resultado else "Tudo OK. Sem erros e vulnerabilidades detetadas!"
    
    # Salvar o arquivo completo para visualização
    caminho_arquivo = os.path.join('uploads', 'resultado.txt')
    with open(caminho_arquivo, 'w', encoding='utf-8') as f:
        f.write(f"Nome do ficheiro analisado: {nome_ficheiro}\n")
        if resultado:
            for linha in resultado:
                f.write(linha + '\n')
        else:
            f.write("Tudo OK. Sem erros e vulnerabilidades detetadas!\n")
    
    return render_template_string('''
    <!DOCTYPE html>
    <html lang="pt">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>Resultado da Análise</title>
        <link href="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css" rel="stylesheet">
    </head>
    <body>
        <div class="container mt-5">
            <h1 class="text-center">Resultado da Análise</h1>
            <pre class="bg-light p-3">{{ primeiras_linhas }}</pre>
            <div class="text-center">
                <a href="{{ url_for('visualizar', filename='resultado.txt') }}" target="_blank" class="btn btn-primary">Ver Análise Completa</a>
                <a href="{{ url_for('index') }}" class="btn btn-secondary">Voltar à Página Inicial</a>
            </div>
        </div>
    </body>
    </html>
    ''', primeiras_linhas=primeiras_linhas)

@app.route('/visualizar/<filename>', methods=['GET'])
def visualizar(filename):
    caminho_arquivo = os.path.join('uploads', filename)
    return send_from_directory(directory='uploads', path=filename)

if __name__ == '__main__':
    if not os.path.exists('uploads'):
        os.makedirs('uploads')
    app.run(debug=True)