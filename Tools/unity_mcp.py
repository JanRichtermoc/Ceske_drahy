#!/usr/bin/env python3
"""Small stdio MCP client for the installed Unity CLI. No private config required."""
import json,os,select,subprocess,sys,time
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
p=subprocess.Popen([os.path.expanduser('~/.unity/bin/Unity'),'mcp','--project-path',ROOT],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,bufsize=1)
def call(method,params,id=1):
 p.stdin.write(json.dumps({'jsonrpc':'2.0','id':id,'method':method,'params':params})+'\n');p.stdin.flush()
 end=time.time()+120
 while time.time()<end:
  if not select.select([p.stdout],[],[],1)[0]:continue
  line=p.stdout.readline()
  if not line:raise RuntimeError('MCP server exited')
  try:r=json.loads(line)
  except ValueError:continue
  if r.get('id')==id:return r
 raise TimeoutError(method)
try:
 call('initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'railway-dev','version':'1.0'}})
 p.stdin.write(json.dumps({'jsonrpc':'2.0','method':'notifications/initialized'})+'\n');p.stdin.flush()
 if len(sys.argv)==1:r=call('tools/list',{},2)
 else:r=call('tools/call',{'name':sys.argv[1],'arguments':json.loads(sys.argv[2]) if len(sys.argv)>2 else {}},2)
 print(json.dumps(r,ensure_ascii=False,indent=2))
finally:p.terminate()
