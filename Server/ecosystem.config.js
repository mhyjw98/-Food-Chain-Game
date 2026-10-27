// pm2로 실행할 때의 설정: 서버가 오류로 종료되면 자동으로 다시 켠다
//   npm install -g pm2
//   pm2 start ecosystem.config.js
//   pm2 logs room-code-server   (로그 보기)
//   pm2 save && pm2 startup     (PC를 켤 때 자동 실행, 선택)
module.exports = {
    apps: [
        {
            name: "room-code-server",
            script: "server.js",
            cwd: __dirname,
            autorestart: true,
            // 켜자마자 계속 죽으면(설정 오류 등) 무한 재시작하지 않게 한다
            max_restarts: 10,
            min_uptime: "10s",
            restart_delay: 2000,
            kill_timeout: 6000,
            env: {
                PORT: 3000,
            },
        },
    ],
};
