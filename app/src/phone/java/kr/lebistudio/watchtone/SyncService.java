package kr.lebistudio.watchtone;
import android.content.SharedPreferences;
import com.google.android.gms.wearable.*;

public final class SyncService extends WearableListenerService {
    @Override public void onDataChanged(DataEventBuffer events){
        SharedPreferences p=getSharedPreferences("watchtone",MODE_PRIVATE);
        for(DataEvent e:events){
            if(e.getType()!=DataEvent.TYPE_CHANGED || !Transfer.STATUS.equals(e.getDataItem().getUri().getPath()))continue;
            DataMap m=DataMapItem.fromDataItem(e.getDataItem()).getDataMap();
            if(m.getString("id","").equals(p.getString("id","-")))
                p.edit().putString("message",m.getString("message","워치 상태를 확인해 주세요.")).commit();
        }
    }
}