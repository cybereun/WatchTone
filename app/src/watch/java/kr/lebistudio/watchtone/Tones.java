package kr.lebistudio.watchtone;

import android.content.*;
import android.media.RingtoneManager;
import android.net.Uri;
import android.os.Environment;
import android.provider.MediaStore;
import android.provider.Settings;
import com.google.android.gms.wearable.*;
import java.io.*;

final class Tones {
    final Context ctx;
    final ContentResolver cr;
    final SharedPreferences prefs;
    static final int TYPE=RingtoneManager.TYPE_RINGTONE;
    static final String MANUAL="워치에 저장했습니다. 워치벨을 열어 벨소리를 적용해 주세요.";
    Tones(Context c){ctx=c.getApplicationContext();cr=ctx.getContentResolver();prefs=ctx.getSharedPreferences("watchtone",0);}
    Uri current(){return RingtoneManager.getActualDefaultRingtoneUri(ctx,TYPE);}
    static String encode(Uri u){return u==null?"":u.toString();}
    Uri decoded(String key){String s=prefs.getString(key,"");return s.isEmpty()?null:Uri.parse(s);}
    Uri plain(Uri u){
        if(u==null)return null;
        try{Uri raw=cr.uncanonicalize(u);if(raw!=null)u=raw;}catch(RuntimeException ignored){}
        String authority=u.getAuthority();
        if(authority!=null && authority.matches("[0-9]+@media"))authority="media";
        Uri plain=u.buildUpon().authority(authority).clearQuery().fragment(null).build();
        if("media".equals(authority) && plain.getPathSegments().size()==4 &&
                "external_primary".equals(plain.getPathSegments().get(0)) &&
                "audio".equals(plain.getPathSegments().get(1)) &&
                "media".equals(plain.getPathSegments().get(2))){
            return ContentUris.withAppendedId(MediaStore.Audio.Media.EXTERNAL_CONTENT_URI,ContentUris.parseId(plain));
        }
        return plain;
    }
    boolean same(Uri a,Uri b){return java.util.Objects.equals(plain(a),plain(b));}
    private Uri pickerUri(Uri stored){
        // Samsung's ringtone picker matches the canonical URI from the external audio list.
        Uri external=plain(stored);
        Uri canonical=cr.canonicalize(external);
        return canonical==null ? external : canonical;
    }
    boolean available(Uri u){
        if(u==null)return false;
        try(android.os.ParcelFileDescriptor fd=cr.openFileDescriptor(u,"r")){return fd!=null;}
        catch(Exception ignored){return false;}
    }
    Uri store(File file,String title,String sha) throws Exception {
        String key="media_"+sha;Uri existing=decoded(key);
        if(available(existing))return existing;
        String stem=title.replaceAll("(?i)\\.mp3$","").replaceAll("[\\p{Cntrl}\\\\/:*?\"<>|]","_").trim();
        if(stem.isEmpty())stem="내 벨소리";
        if(stem.length()>64)stem=stem.substring(0,64);
        ContentValues values=new ContentValues();
        values.put(MediaStore.Audio.Media.DISPLAY_NAME,stem+".mp3");
        values.put(MediaStore.Audio.Media.TITLE,stem);
        values.put(MediaStore.Audio.Media.MIME_TYPE,"audio/mpeg");
        values.put(MediaStore.Audio.Media.RELATIVE_PATH,Environment.DIRECTORY_RINGTONES+"/워치벨/");
        values.put(MediaStore.Audio.Media.IS_RINGTONE,1);
        values.put(MediaStore.Audio.Media.IS_PENDING,1);
        Uri u=cr.insert(MediaStore.Audio.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY),values);
        if(u==null)throw new IOException("벨소리 저장 공간을 열 수 없습니다.");
        boolean finished=false;
        try{
            try(InputStream in=new FileInputStream(file);OutputStream out=cr.openOutputStream(u)){
                if(out==null)throw new IOException("벨소리 파일을 만들 수 없습니다.");
                Transfer.copy(in,out);
            }
            values.clear();values.put(MediaStore.Audio.Media.IS_PENDING,0);
            if(cr.update(u,values,null,null)!=1)throw new IOException("벨소리 파일을 등록하지 못했습니다.");
            prefs.edit().putString(key,u.toString()).commit();finished=true;return u;
        }finally{if(!finished)cr.delete(u,null,null);}
    }
    void receive(InputStream input,String title,String sha,String id) throws Exception{
        File tmp=File.createTempFile("received-",".mp3",ctx.getCacheDir());
        try{
            String actual;
            try(FileOutputStream out=new FileOutputStream(tmp)){actual=Transfer.copy(input,out);}
            if(sha!=null && !actual.equals(sha))throw new IOException("전송된 파일이 손상되었습니다.");
            Transfer.validateMp3(tmp);
            Uri u=store(tmp,title,actual);
            Uri before=current();
            SharedPreferences.Editor edit=prefs.edit().putString("uri",u.toString()).putString("name",title).putString("id",id);
            if(!prefs.contains("original") || !same(before,decoded("lastApplied")))edit.putString("original",encode(before));
            edit.commit();
            apply();
        }finally{tmp.delete();}
    }
    void report(String msg){
        prefs.edit().putString("status",msg).commit();
        publish(prefs.getString("id",""),msg);
    }
    void publish(String id,String msg){
        if(!id.isEmpty())Wearable.getDataClient(ctx).putDataItem(Transfer.status(id,msg).asPutDataRequest().setUrgent());
    }
    void apply(){
        Uri target=decoded("uri");if(target==null)return;
        try{
            if(!available(target)){report("저장된 곡을 열 수 없습니다. 휴대폰에서 다시 보내 주세요.");return;}
            Uri pickerTarget=pickerUri(target);
            if(!available(pickerTarget)){report("워치 벨소리 목록에서 곡을 열 수 없습니다. 휴대폰에서 다시 보내 주세요.");return;}
            Uri before=current();
            if(!pickerTarget.equals(before)){
                if(!Settings.System.canWrite(ctx)){report(MANUAL);return;}
                if(!prefs.contains("original") || !same(before,decoded("lastApplied")))
                    prefs.edit().putString("original",encode(before)).commit();
                RingtoneManager.setActualDefaultRingtoneUri(ctx,TYPE,pickerTarget);
            }
            if(pickerTarget.equals(current())){
                prefs.edit().putString("lastApplied",target.toString()).commit();
                report("워치 기본 벨소리에 적용됐습니다. 실제 전화로 소리를 확인해 주세요.");
            }else report("파일이 저장됐습니다. 워치 설정에서 곡을 직접 선택해 주세요.");
        }catch(RuntimeException e){report("워치에 저장했습니다. 설정 → 소리 및 진동 → 벨소리에서 곡을 선택해 주세요.");}
    }
    void restore(){
        if(!prefs.contains("original"))return;
        Uri old=decoded("original");
        try{
            RingtoneManager.setActualDefaultRingtoneUri(ctx,TYPE,old);
            if(!same(old,current())){report("복원을 확인하지 못했습니다. 워치 설정에서 확인해 주세요.");return;}
            prefs.edit().remove("lastApplied").remove("original").commit();
            report("이전 벨소리로 복원했습니다.");
        }catch(RuntimeException e){report("복원하려면 워치의 시스템 설정 변경 권한을 확인해 주세요.");}
    }
}