# ==========================================
# STAGE 1: Build
# ==========================================
FROM node:20-alpine AS build
WORKDIR /app

COPY battle-viewer-react-app/package.json battle-viewer-react-app/package-lock.json ./

RUN npm ci

COPY battle-viewer-react-app/ .

RUN npm run build

# ==========================================
# STAGE 2: Runtime (nginx)
# ==========================================
FROM nginx:1.27-alpine AS final

COPY --from=build /app/build /usr/share/nginx/html

# SPA routing + reverse proxy naar backend
RUN printf 'server {\n\
    listen 80;\n\
    root /usr/share/nginx/html;\n\
    index index.html;\n\
\n\
    # REST API proxy\n\
    location /api/ {\n\
        proxy_pass http://backend:8080/api/;\n\
        proxy_http_version 1.1;\n\
        proxy_set_header Host $host;\n\
        proxy_set_header X-Real-IP $remote_addr;\n\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n\
    }\n\
\n\
    # SignalR proxy (WebSocket + long-polling)\n\
    location /battlehub {\n\
        proxy_pass http://backend:8080/battlehub;\n\
        proxy_http_version 1.1;\n\
        proxy_set_header Upgrade $http_upgrade;\n\
        proxy_set_header Connection "upgrade";\n\
        proxy_set_header Host $host;\n\
        proxy_set_header X-Real-IP $remote_addr;\n\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n\
        proxy_cache_bypass $http_upgrade;\n\
        proxy_read_timeout 86400s;\n\
    }\n\
\n\
    # SPA routing\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
}\n' > /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]