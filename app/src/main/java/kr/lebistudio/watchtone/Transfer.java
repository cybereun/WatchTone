package kr.lebistudio.watchtone;

import android.content.Context;
import android.media.MediaExtractor;
import android.media.MediaFormat;
import com.google.android.gms.wearable.*;
import java.io.*;
import java.security.MessageDigest;

final class Transfer {
    static final String REQUEST="/watchtone/request", STATUS="/watchtone/status", CAP="watchtone_receiver_v1";
    static final long MAX=32L*1024*1024;
    static String copy(InputStream in, OutputStream out) throws Exception {
        if(in==null)throw new IOException("파일을 열 수 없습니다.");
        MessageDigest digest=MessageDigest.getInstance("SHA-256");
        byte[] buf=new byte[32768];int n;long count=0;
        while((n=in.read(buf))!=-1){
            count+=n;if(count>MAX)throw new IOException("32MB 이하의 MP3를 선택해 주세요.");
            out.write(buf,0,n);digest.update(buf,0,n);
        }
        if(count==0)throw new IOException("파일이 비어 있습니다.");
        StringBuilder hex=new StringBuilder(64);
        for(byte b:digest.digest())hex.append(String.format(java.util.Locale.ROOT,"%02x",b&255));
        return hex.toString();
    }
    static void validateMp3(File f) throws IOException {
        MediaExtractor ex=new MediaExtractor();
        try{
            ex.setDataSource(f.getAbsolutePath());
            for(int i=0;i<ex.getTrackCount();i++){
                if("audio/mpeg".equals(ex.getTrackFormat(i).getString(MediaFormat.KEY_MIME)))return;
            }
            throw new IOException("MP3 형식의 오디오 파일을 선택해 주세요.");
        }finally{ex.release();}
    }
    static PutDataMapRequest status(String id,String message){
        PutDataMapRequest r=PutDataMapRequest.create(STATUS);
        r.getDataMap().putString("id",id);r.getDataMap().putString("message",message);
        return r;
    }
}