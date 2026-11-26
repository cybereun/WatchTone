package kr.lebistudio.watchtone;
import com.google.android.gms.tasks.Tasks;
import com.google.android.gms.wearable.*;
import java.io.*;
import java.util.concurrent.TimeUnit;

public final class SyncService extends WearableListenerService {
    @Override public void onDataChanged(DataEventBuffer events){
        for(DataEvent e:events){
            if(e.getType()!=DataEvent.TYPE_CHANGED || !Transfer.REQUEST.equals(e.getDataItem().getUri().getPath()))continue;
            DataMap m=DataMapItem.fromDataItem(e.getDataItem()).getDataMap();
            String id=m.getString("id","");
            if(id.isEmpty())continue;
            Tones t=new Tones(this);
            try{
                String local=Tasks.await(Wearable.getNodeClient(this).getLocalNode(),15,TimeUnit.SECONDS).getId();
                if(!local.equals(m.getString("target")))continue;
                synchronized(Tones.class){
                    if(id.equals(t.prefs.getString("id",""))){
                        t.publish(id,t.prefs.getString("status",Tones.MANUAL));continue;
                    }
                    Asset asset=m.getAsset("audio");if(asset==null)throw new IOException("파일 누락");
                    DataClient.GetFdForAssetResponse response=Tasks.await(Wearable.getDataClient(this).getFdForAsset(asset),90,TimeUnit.SECONDS);
                    try(InputStream in=response.getInputStream()){
                        t.receive(in,m.getString("name","내 벨소리"),m.getString("sha",""),id);
                    }finally{response.release();}
                }
            }catch(Exception ex){t.publish(id,"워치에서 곡을 저장하지 못했습니다. 저장 공간과 연결을 확인하고 다시 보내 주세요.");}
        }
    }
}