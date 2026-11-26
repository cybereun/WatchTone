package kr.lebistudio.watchtone;

import android.content.*;
import android.net.Uri;
import android.os.Bundle;
import android.provider.Settings;
import java.io.*;

public final class MainActivity extends Screen {
    Tones tones;
    private String pending="",pendingUri="";
    @Override public void onCreate(Bundle b){
        super.onCreate(b);tones=new Tones(this);
        if(b!=null){pending=b.getString("pending","");pendingUri=b.getString("pendingUri","");}
    }
    @Override protected void onSaveInstanceState(Bundle b){
        super.onSaveInstanceState(b);b.putString("pending",pending);b.putString("pendingUri",pendingUri);
    }
    @Override protected void onResume(){
        super.onResume();
        if(!pending.isEmpty()){
            String action=pending;pending="";
            if(Settings.System.canWrite(this) && pendingUri.equals(prefs.getString("uri","")))perform(action);
        }
        render();
    }
    void render(){
        clear();label("워치벨",22,ACCENT,true,2);
        String uri=prefs.getString("uri","");
        if(uri.isEmpty()){
            label("휴대폰에서 곡을 보내세요",17,INK,true,8);
            label("테스트 곡으로 먼저 벨소리를 확인할 수 있어요.",13,MUTED,false,5);
            button("테스트 벨소리 사용",true,()->{
                try(InputStream in=getResources().openRawResource(R.raw.bell_sample)){
                    synchronized(Tones.class){tones.receive(in,"워치벨 테스트.mp3",null,"");}
                    if(!Settings.System.canWrite(this))permission("apply");
                }catch(Exception ex){notice("테스트 벨소리를 저장할 수 없습니다.");}
                render();
            });
            return;
        }
        label(prefs.getString("name","내 벨소리"),18,INK,true,7);
        label(prefs.getString("status","저장 중…"),13,MUTED,false,5);
        button("벨소리 적용",true,()->permission("apply"));
        button(previewing()?"미리 듣기 중지":"미리 듣기",false,()->preview(Uri.parse(uri)));
        button("벨소리 설정 열기",false,this::soundSettings);
        if(prefs.contains("original"))button("이전 벨소리 복원",false,()->permission("restore"));
    }
    void perform(String action){synchronized(Tones.class){if("restore".equals(action))tones.restore();else tones.apply();}}
    void permission(String action){
        if(Settings.System.canWrite(this)){perform(action);return;}
        try{
            pending=action;pendingUri=prefs.getString("uri","");
            startActivity(new Intent(Settings.ACTION_MANAGE_WRITE_SETTINGS,Uri.parse("package:"+getPackageName())));
        }catch(ActivityNotFoundException|SecurityException e){
            pending="";notice("권한 화면을 열 수 없습니다. 소리 및 진동에서 벨소리를 선택해 주세요.");soundSettings();
        }
    }
    void soundSettings(){
        try{startActivity(new Intent(Settings.ACTION_SOUND_SETTINGS));}
        catch(ActivityNotFoundException|SecurityException e){
            try{startActivity(new Intent(Settings.ACTION_SETTINGS));}catch(RuntimeException ignored){}
            notice("설정 → 소리 및 진동 → 벨소리를 열어 주세요.");
        }
    }
}