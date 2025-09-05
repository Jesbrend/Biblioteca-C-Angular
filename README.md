📚 Sistema de Biblioteca

Este projeto é um sistema de gerenciamento de biblioteca desenvolvido em Angular (front-end) e .NET 9 (back-end).
O objetivo é permitir que bibliotecários cadastrem usuários, gerenciem livros e aprovem empréstimos, enquanto usuários leitores podem solicitar e devolver livros pelo sistema.

🚀 Tecnologias Utilizadas
Front-end (Angular)

Angular 16+ — Framework front-end.

RxJS — Programação reativa.

Angular Forms — Reactive Forms.

Signals — Estado reativo no Angular.

SCSS/CSS — Estilização.

Back-end (.NET)

ASP.NET Core 9 — API REST.

Entity Framework Core — ORM para acesso ao banco.

Npgsql — Provider PostgreSQL.

JWT (JSON Web Tokens) — Autenticação e autorização.

Swashbuckle (Swagger) — Documentação da API.

Banco de Dados

PostgreSQL 15+ (produção)

(Pode ser adaptado para SQLite em cenários de teste)

📂 Estrutura do Projeto
Front-end (biblioteca-app/src/app)

core/ → Guards, interceptors, models, services, utils.

auth.service.ts → Autenticação (login/logout, gerenciamento de token JWT).

user.service.ts → CRUD de usuários (AppUsers).

book.service.ts → CRUD de livros.

loan.service.ts → CRUD de empréstimos.

features/ → Páginas principais (empréstimos, usuários, approvals, etc).

loans-page.component.ts → Tela de empréstimos, com lógica diferenciada para bibliotecários e leitores.

register-page.component.ts → Tela de cadastro.

app.component.ts / app.html → Layout principal com controle de topbar baseado em autenticação.

interceptors/ → auth.interceptor.ts insere o token JWT nas requisições.

Back-end (Biblioteca.Api)

Program.cs → Configuração da aplicação, rotas e middleware.

Data/ → BibliotecaDbContext com EF Core.

Models/ → Entidades (AppUser, User, Book, Loan, Shelf, Placement).

Contracts/ → DTOs (LoginDto, RegisterDto, LoanCreateDto, etc).

Utils/AuthUtils.cs → Geração de tokens JWT e hash de senha.

Migrations/ → Controle de esquema do banco via EF Core.

🔑 Autenticação e Autorização

O sistema usa JWT para autenticação.
Cada usuário tem um papel (role):

USER → Leitor padrão, pode:

Solicitar empréstimos.

Visualizar apenas seus próprios empréstimos em aberto e histórico.

LIBRARIAN → Bibliotecário, pode:

Criar usuários (com senha vinculada em AppUsers).

Gerenciar livros, estantes e alocações.

Aprovar/rejeitar empréstimos.

Realizar devoluções de qualquer usuário.

O token JWT contém claims:

NameIdentifier → ID do usuário.

Name → Nome do usuário.

Email → E-mail do usuário.

Role → Papel (USER / LIBRARIAN).

🛠️ Como Rodar o Projeto
Pré-requisitos

Node.js v18+

Angular CLI 16+

.NET SDK 9.0.304+

PostgreSQL 15+

🔹 Rodando o Back-end (.NET API)

Clone o repositório:

git clone https://github.com/seu-usuario/biblioteca.git
cd biblioteca/Biblioteca.Api


Edite o arquivo appsettings.json:

{
  "Jwt": {
    "Key": "sua_chave_secreta_com_no_minimo_32_caracteres",
    "Issuer": "BibliotecaApi",
    "Audience": "BibliotecaApp"
  },
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=BibliotecaDb;Username=postgres;Password=suasenha"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}


Execute as migrations e rode a API:

dotnet ef database update
dotnet run


Acesse no navegador:
👉 https://localhost:5001/swagger

🔹 Rodando o Front-end (Angular)
cd biblioteca-app
npm install


Edite o arquivo src/environments/environment.ts:

export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};


Rode a aplicação:

ng serve


Acesse no navegador:
👉 http://localhost:4200

⚙️ Funcionalidades

Login / Registro

Usuário faz login com e-mail e senha → Recebe JWT.

Bibliotecário pode registrar novos usuários com senha.

Bibliotecário

Criar/editar usuários (AppUsers).

Gerenciar livros (criar, editar, excluir).

Gerenciar estantes e alocações.

Aprovar/rejeitar empréstimos.

Devolver livros de qualquer usuário.

Leitor

Solicitar empréstimos apenas para si mesmo.

Visualizar apenas seus empréstimos em aberto e histórico.

Realizar devolução de seus próprios livros.

Segurança

API só aceita requisições com JWT válido.

Controle de acesso baseado em roles (guards no Angular + políticas no .NET).

📖 Exemplos de Rotas da API
Auth

POST /api/auth/login → Faz login e retorna JWT.

POST /api/auth/register → Registra novo usuário.

Users

GET /api/users → Lista usuários.

POST /api/users → Cria novo usuário.

Books

GET /api/books → Lista livros.

POST /api/books → Cria novo livro.

Loans

POST /api/loans → Cria empréstimo.

PUT /api/loans/{id}/approve → Aprova empréstimo (bibliotecário).

PUT /api/loans/{id}/reject → Rejeita empréstimo (bibliotecário).

PUT /api/loans/{id}/return → Registra devolução.

📖 Fluxo de Uso

Bibliotecário inicial é criado automaticamente:

Email: admin@local

Senha: admin123

Bibliotecário cadastra usuários em Usuários (com senha).

Leitores fazem login → solicitam livros → bibliotecária aprova ou rejeita.

Histórico e devoluções ficam registrados.

🤝 Contribuição

Faça um fork do projeto.

Crie uma branch para sua feature:

git checkout -b minha-feature


Faça commit das mudanças:

git commit -m 'Adicionei nova feature'


Faça push para sua branch:

git push origin minha-feature


Abra um Pull Request.

📜 Licença

Este projeto é de uso educacional/demonstrativo.
Pode ser adaptado livremente para fins acadêmicos ou pessoais.
