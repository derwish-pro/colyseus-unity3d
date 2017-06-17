import { Room } from "../colyseus";

export class ChatRoom extends Room<any> {

  constructor(options) {
    super(options);
    this.setPatchRate(50);
    this.setState({ messages: [], users: [] });
    console.log("ChatRoom created!", options);
  }

  onJoin(client) {
    this.state.messages.push(`${client.id} joined.`);
    this.state.users.push({ id: client.id, a: "b" }); //joinTime: new Date() };
    console.log("Client", client.id, "joined");
  }

  onLeave(client) {
    this.state.messages.push(`${client.id} left.`);
    // delete this.state.users[client.id];
  }

  onMessage(client, data) {
    this.state.messages.push(data.message);
    console.log("ChatRoom:", client.id, data);
  }

  onDispose() {
    console.log("Dispose ChatRoom");
  }

}
