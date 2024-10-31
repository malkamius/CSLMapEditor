var s=class{constructor(){this.Enabled=!1}},c=class extends s{},y=class extends s{},l=class{constructor(){this.NewInput=new Uint8Array;this.Response=new Uint8Array}},d=class{constructor(){this.Options=[];this.SupportedClientTypes=["256COLOR","VT100","ANSI","TRUECOLOR"];this.NegotiatedClientTypes=[];this.currentTypeIndex=-1;this.ClientNegotiateTelnetType=new Uint8Array([255,250,24,0])}IsNegotiationRequired(e){return e.includes(255)}Negotiate(e){let t=new l,r=0,a=0;for(;r<e.length;)if(e[r]===255){if(t.NewInput=this.concatUint8Arrays(t.NewInput,e.slice(a,r)),r++,r>=e.length)break;let n=e[r];switch(r++,n){case 253:case 254:case 251:case 252:if(r>=e.length)break;let o=e[r];r++,t.Response=this.concatUint8Arrays(t.Response,this.handleCommand(n,o));break;case 250:let u=this.handleSubNegotiation(e.slice(r));t.Response=this.concatUint8Arrays(t.Response,u),r+=this.findSubNegotiationEnd(e.slice(r))+1;break}a=r}else r++;return t.NewInput=this.concatUint8Arrays(t.NewInput,e.slice(a)),t}handleCommand(e,t){switch(t){case 24:if(e===253||e===251)return this.SendNextClientType();break}return new Uint8Array}handleSubNegotiation(e){return e[0]===24&&e[1]===1?this.SendNextClientType():new Uint8Array}findSubNegotiationEnd(e){for(let t=0;t<e.length-1;t++)if(e[t]===255&&e[t+1]===240)return t+1;return e.length}SendNextClientType(){this.currentTypeIndex=(this.currentTypeIndex+1)%this.SupportedClientTypes.length;let e=this.SupportedClientTypes[this.currentTypeIndex],t=new TextEncoder().encode(e);return this.concatUint8Arrays(this.ClientNegotiateTelnetType,t,new Uint8Array([255,240]))}concatUint8Arrays(...e){let t=e.reduce((n,o)=>n+o.length,0),r=new Uint8Array(t),a=0;for(let n of e)r.set(n,a),a+=n.length;return r}};export{c as EchoOption,l as NegotiateResponse,y as SupressGoAhead,d as TelnetNegotiator,s as TelnetOption};
//# sourceMappingURL=telnet_negotiation.js.map