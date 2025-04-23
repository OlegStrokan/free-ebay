FROM node:20

WORKDIR /app

COPY package.json ./
COPY yarn.lock ./

RUN yarn

COPY . . 

RUN yarn build

WORKDIR /src
ENV TZ=Europe/Prague
COPY --from=BASEIMAGE /src/dist /src/dist
COPY --from=BASEIMAGE /src/node_modules /src/node_modules
EXPOSE 3000

CMD ["node", "dist/main.js"]