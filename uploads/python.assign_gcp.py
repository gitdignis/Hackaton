import pyodbc
import os

# Global variables
ficheiro_gcp = "./atribuir.gcp.txt"
ServerAddress2 = "10.18.54.186"
DatabaseName2 = "MYDASHBOARD"
DBConnectionString2 = (
    f"DRIVER={{SQL Server}};SERVER={ServerAddress2};DATABASE={DatabaseName2};"
    f"Uid=AUTOMATION_BOT;Pwd=Automan2019;WSID={os.path.basename(__file__)};"
)
emailcc = "jose.a.machado@telecom.pt"

gcp_sucesso = 0
gcp_erro = 0
gcp_novos = 0
gcp_alteracoes = 0
officepack = 0
lista_gcps = []
mensagens = None

lt = None
lttipo = None
ltdesc = None
ltgpid = None

# Create SQL connection
adCNIOP = pyodbc.connect(DBConnectionString2)

# STARTUP Functions
def GetDataFiles():
    global lista_gcps
    print("GetDataFiles (Start)")
    try:
        with open(ficheiro_gcp, 'r') as file:
            lista_gcps = file.readlines()
    except FileNotFoundError:
        print(f"Error: {ficheiro_gcp} - File doesn't exist or can't be read")
    print("GetDataFiles (Completed)")

def CountGCP():
    global gcp_novos, gcp_alteracoes, officepack
    print("CountGCP (START)")
    for n, line in enumerate(lista_gcps[1:], start=1):
        gcp = line.strip().split(',')
        if len(gcp) != 4 and not line.strip().startswith("#"):
            print(f"Error: Incorrect number of arguments in line: {n}")
            exit()
        elif not line.strip().startswith("#"):
            if gcp[2] == '1':
                gcp_novos += 1
            elif gcp[2] == '2':
                gcp_alteracoes += 1
            elif gcp[2] == '7':
                officepack += 1
    print(f"CountGCP (Completed) - Total: {gcp_novos + gcp_alteracoes} Novos: {gcp_novos} Alterações: {gcp_alteracoes}")

def AssignLT():
    global lt, ltgpid
    adCNIOP.open(DBConnectionString2)
    if adCNIOP is None:
        print("ERROR: Failed to connect to the database")
        exit()
    for i in range(1, len(lista_gcps)):
        if GetRequestData(i):
            sQuery = f"EXEC automation.proc_insert_job_assign_opportunity '{lt}','{ltgpid}','{os.getenv('USERDOMAIN')}\\{os.getenv('USERNAME').upper()}'"
            print(sQuery)
            adCNIOP.execute(sQuery)
    adCNIOP.close()
    return True

def AssignNo():
    global lt, lttipo, ltdesc, ltgpid
    print("AssignNo (Start)")
    adCNIOP.open(DBConnectionString2)
    if adCNIOP is None:
        print("ERROR: Failed to connect to the database")
        exit()
    print("AssignNo - DB Connection - (Open)")
    for i in range(1, len(lista_gcps)):
        print(f"AssignNO - Request {i} - (Start)")
        GetRequestData(i)
        if lttipo == '-1':
            print("AssignNo - Only LT (Skip)")
        elif lttipo == '7':
            RequestParameters = f'{{"Action Type":"Create","Project Manager (Id)":"{ltgpid}","Business Type":"{lttipo}","Adicional Notification emails":"jose-a-machado@telecom.pt; carla-c-fonseca@telecom.pt","Opportunity Description":"{ltdesc}"}}'
            sQuery = f"EXEC automation.proc_insert_job_project_distribuition '{lt}','Create_Create','{ltgpid}','{os.getenv('USERDOMAIN')}\\{os.getenv('USERNAME').upper()}','{ltdesc}'"
            print(sQuery)
            adCNIOP.execute(sQuery)
        else:
            RequestParameters = f'{{"Action Type":"CreateGCP","Project Manager (Id)":"{ltgpid}","Business Type":"{lttipo}","Adicional Notification emails":"jose-a-machado@telecom.pt; carla-c-fonseca@telecom.pt","Opportunity Description":"{ltdesc}"}}'
            sQuery = f"EXEC automation.proc_insert_job_create_project '{lt}','{os.getenv('USERDOMAIN')}\\{os.getenv('USERNAME').upper()}','{RequestParameters}',4,'GCP'"
            print(sQuery)
            adCNIOP.execute(sQuery)
    adCNIOP.close()
    return True

def GetRequestData(n):
    global lt, lttipo, ltdesc, ltgpid
    print(f"GetRequestData (Start): {n}/{len(lista_gcps) - 1}")
    gcp = lista_gcps[n].strip().split(',')
    if lista_gcps[n].strip().startswith("#"):
        print("GetRequestData (SKIP): Commented Line")
        return False
    else:
        lt = gcp[0]
        lttipo = gcp[1]
        ltdesc = gcp[2]
        ltgpid = gcp[3]
        if int(lttipo) < 1:
            print(f"GetRequestData Type {lttipo} Bypass (Completed): {lt}")
            return False
        print(f"GetRequestData (Completed): {lt}")
        return True

# Main execution
GetDataFiles()
CountGCP()
print(f"{gcp_novos + gcp_alteracoes} Packs/ {gcp_novos} Novos/ {gcp_alteracoes} Alteracoes")
AssignLT()
AssignNo()
print("Pedidos de atribuição enviados para o Servidor!")