Frontend Vite+React.

Como executar localmente:
- npm install
- npm start

Observações:
- O projeto usa Vite. O comando npm start inicia o servidor de desenvolvimento em http://localhost:5173.
- As chamadas de API estão configuradas para http://localhost:5000. Certifique-se de que a API (Rider/.NET) esteja rodando nessa porta, ou ajuste os endpoints no código conforme necessário.
- Se precisar usar HMR via WSS (por exemplo, em ambiente remoto com proxy HTTPS), defina a variável de ambiente VITE_USE_WSS=true antes de iniciar: VITE_USE_WSS=true npm start
