export class CdpClient {
  constructor(webSocketUrl, { WebSocketImpl = globalThis.WebSocket, timeoutMs = 30000 } = {}) {
    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) throw new Error("CDP timeout must be positive");
    this.nextId = 1;
    this.pending = new Map();
    this.events = [];
    this.timeoutMs = timeoutMs;
    this.socket = new WebSocketImpl(webSocketUrl);
    this.socket.addEventListener("message", event => this.handleMessage(event));
    this.socket.addEventListener("close", () => this.failPending(new Error("CDP connection closed")));
    this.socket.addEventListener("error", () => this.failPending(new Error("CDP connection error")));
  }

  async open() {
    if (this.socket.readyState === 1) return;
    if (this.socket.readyState !== 0) throw new Error("CDP connection closed before opening");
    await new Promise((resolveOpen, rejectOpen) => {
      const finish = error => {
        clearTimeout(timer);
        this.socket.removeEventListener("open", onOpen);
        this.socket.removeEventListener("error", onError);
        this.socket.removeEventListener("close", onClose);
        error ? rejectOpen(error) : resolveOpen();
      };
      const onOpen = () => finish();
      const onError = () => finish(new Error("CDP connection error"));
      const onClose = () => finish(new Error("CDP connection closed before opening"));
      const timer = setTimeout(() => finish(new Error("CDP connection timed out")), this.timeoutMs);
      this.socket.addEventListener("open", onOpen);
      this.socket.addEventListener("error", onError);
      this.socket.addEventListener("close", onClose);
    });
  }

  handleMessage(event) {
    const message = JSON.parse(event.data);
    if (message.id && this.pending.has(message.id)) {
      const { resolveCommand, rejectCommand } = this.pending.get(message.id);
      this.pending.delete(message.id);
      if (message.error) {
        rejectCommand(new Error(`${message.error.message}: ${message.error.data ?? ""}`));
        return;
      }
      resolveCommand(message.result ?? {});
      return;
    }
    if (["Runtime.consoleAPICalled", "Runtime.exceptionThrown", "Log.entryAdded"].includes(message.method)) {
      this.events.push(message);
    }
  }

  async send(method, params = {}) {
    if (this.socket.readyState !== 1) throw new Error(`CDP connection is not open: ${method}`);
    const id = this.nextId++;
    return await new Promise((resolveCommand, rejectCommand) => {
      const settle = (error, result) => {
        clearTimeout(timer);
        this.pending.delete(id);
        error ? rejectCommand(error) : resolveCommand(result);
      };
      const timer = setTimeout(() => settle(new Error(`CDP command timed out: ${method}`)), this.timeoutMs);
      this.pending.set(id, {
        resolveCommand: result => settle(null, result),
        rejectCommand: error => settle(error),
      });
      try { this.socket.send(JSON.stringify({ id, method, params })); }
      catch (error) { settle(error); }
    });
  }

  failPending(error) {
    for (const request of this.pending.values()) request.rejectCommand(error);
  }

  close() {
    this.failPending(new Error("CDP connection closed"));
    this.socket.close();
  }
}
