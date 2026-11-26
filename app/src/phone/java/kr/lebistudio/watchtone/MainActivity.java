package kr.lebistudio.watchtone;

import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.*;
import android.provider.OpenableColumns;
import com.google.android.gms.tasks.Tasks;
import com.google.android.gms.wearable.*;
import java.io.*;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.*;

public final class MainActivity extends Screen {
    private static final ExecutorService worker=Executors.newSingleThreadExecutor();
    private static volatile boolean busy;
    private File song(){return new File(getFilesDir(),prefs.getString("file","no-selection"));}
    @Override void render(){
        clear();
        android.widget.ImageView logo=new android.widget.ImageView(this);
        logo.setImageResource(R.drawable.watchtone);page.addView(logo,dp(52),dp(52));
        label("워치벨",34,INK,true,15);
        label("내 MP3를 워치 전화 벨소리로",17,MUTED,false,4);
        label("선택한 곡",14,ACCENT,true,32);
        boolean selected=song().isFile();
        label(selected?prefs.getString("name","MP3"):"곡을 선택하세요",23,INK,true,5);
        label("MP3  ·  최대 32MB",13,MUTED,false,4);
        button(selected?"다른 MP3 선택":"MP3 선택",!selected,()->{
            Intent i=new Intent(Intent.ACTION_OPEN_DOCUMENT).setType("audio/*").addCategory(Intent.CATEGORY_OPENABLE);
            try{startActivityForResult(i,1);}catch(android.content.ActivityNotFoundException e){notice("파일 선택기를 열 수 없습니다.");}
        }).setEnabled(!busy);
        if(selected){
            button(previewing()?"미리 듣기 중지":"미리 듣기",false,()->preview(Uri.fromFile(song())));
            button("워치 벨소리로 설정",true,()->runJob(this::send)).setEnabled(!busy);
        }
        label("전송 상태",14,ACCENT,true,29);
        label(prefs.getString("message","워치에도 워치벨을 설치하고 한 번 열어 주세요."),16,INK,false,6);
    }
    interface Job{void run() throws Exception;}
    private void runJob(Job job){
        if(busy)return;
        busy=true;stopPreview();render();worker.execute(()->{
            try{job.run();}catch(Exception e){
                String msg=e instanceof IOException?e.getMessage():"워치 연결과 두 기기의 Google Play 서비스를 확인한 뒤 다시 시도해 주세요.";
                prefs.edit().putString("message",msg).apply();
            }finally{busy=false;runOnUiThread(()->{if(!isDestroyed())render();});}
        });
    }
    @Override protected void onActivityResult(int code,int result,Intent data){
        super.onActivityResult(code,result,data);
        if(code==1 && result==RESULT_OK && data!=null && data.getData()!=null){
            Uri uri=data.getData();runJob(()->importSong(uri));
        }
    }
    private void importSong(Uri uri) throws Exception {
        File next=File.createTempFile("song-",".mp3",getFilesDir());
        boolean saved=false;
        try{
            String name="내 벨소리.mp3";
            try(Cursor c=getContentResolver().query(uri,new String[]{OpenableColumns.DISPLAY_NAME},null,null,null)){
                if(c!=null && c.moveToFirst() && c.getString(0)!=null)name=c.getString(0);
            }
            String digest;
            try(InputStream in=getContentResolver().openInputStream(uri);OutputStream out=new FileOutputStream(next)){
                digest=Transfer.copy(in,out);
            }
            Transfer.validateMp3(next);
            File old=song();
            saved=prefs.edit().putString("file",next.getName()).putString("name",name).putString("sha",digest)
                .remove("id").putString("message","워치로 보낼 준비가 됐습니다.").commit();
            if(!saved)throw new IOException("선택한 곡을 저장하지 못했습니다.");
            if(old.isFile())old.delete();
        }finally{if(!saved)next.delete();}
    }
    private void send() throws Exception {
        prefs.edit().remove("id").putString("message","연결된 워치를 찾고 있습니다…").commit();
        Set<Node> nodes=Tasks.await(Wearable.getCapabilityClient(this).getCapability(Transfer.CAP,CapabilityClient.FILTER_REACHABLE),20,TimeUnit.SECONDS).getNodes();
        if(nodes.isEmpty())throw new IOException("워치를 찾지 못했습니다. 워치벨을 워치에서 한 번 열고, 휴대폰과 연결한 뒤 다시 눌러 주세요.");
        if(nodes.size()>1)throw new IOException("워치벨을 사용할 워치 한 대만 연결해 주세요.");
        Node node=nodes.iterator().next();String id=UUID.randomUUID().toString();
        File f=song();
        try(ParcelFileDescriptor fd=ParcelFileDescriptor.open(f,ParcelFileDescriptor.MODE_READ_ONLY)){
            PutDataMapRequest req=PutDataMapRequest.create(Transfer.REQUEST);
            DataMap m=req.getDataMap();m.putString("id",id);m.putString("target",node.getId());
            m.putString("name",prefs.getString("name","내 벨소리"));m.putString("sha",prefs.getString("sha",""));
            m.putAsset("audio",Asset.createFromFd(fd));
            prefs.edit().putString("id",id).putString("message",node.getDisplayName()+"에 전송 중 · 워치 응답을 기다립니다.").commit();
            Tasks.await(Wearable.getDataClient(this).putDataItem(req.asPutDataRequest().setUrgent()),90,TimeUnit.SECONDS);
        }
    }
}