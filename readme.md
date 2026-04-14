## Repro steps
1. Setup RabbitMq with the following command
```sh
sudo docker run -d --name some-rabbit \
    -e RABBITMQ_DEFAULT_USER=admin \
    -e RABBITMQ_DEFAULT_PASS=admin \
    -p 15672:15672 \
    -p 5672:5672 \
    docker.io/library/rabbitmq:management
```
2. Run this project
3. Hit the root endpoint with a browser, curl, or by the .http file at (by default) http://localhost:5153/
4. Observe only one attempt by the worker
  - Command is queued
  - Response times out
  - Command is still queued
  - Wolverine shows an exception
  - No retry after 1+ minutes
