package kr.lebistudio.watchtone;

import android.app.Activity;
import android.content.SharedPreferences;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.media.AudioAttributes;
import android.media.MediaPlayer;
import android.net.Uri;
import android.os.Bundle;
import android.view.*;
import android.widget.*;

abstract class Screen extends Activity implements SharedPreferences.OnSharedPreferenceChangeListener {
    static final int BG=0xff101a23, PANEL=0xff20313e, INK=0xfff2f6f7, MUTED=0xffb6c9d1, ACCENT=0xff82e2cc;
    LinearLayout page;
    SharedPreferences prefs;
    private MediaPlayer player;
    abstract void render();

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        prefs=getSharedPreferences("watchtone", MODE_PRIVATE);
        ScrollView scroll=new ScrollView(this);
        scroll.setFillViewport(true);
        scroll.setBackgroundColor(BG);
        page=new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        scroll.addView(page);
        boolean round=getResources().getConfiguration().isScreenRound();
        int pad=round ? Math.round(getResources().getDisplayMetrics().widthPixels*.14f) : dp(23);
        scroll.setClipToPadding(false);
        scroll.setOnApplyWindowInsetsListener((v, insets)->{
            android.graphics.Insets bars=insets.getInsets(WindowInsets.Type.systemBars()|WindowInsets.Type.displayCutout());
            page.setPadding(pad+bars.left, (round?dp(27):dp(26))+bars.top, pad+bars.right, (round?dp(34):dp(26))+bars.bottom);
            return insets;
        });
        setContentView(scroll);
    }
    @Override protected void onStart(){super.onStart(); prefs.registerOnSharedPreferenceChangeListener(this); render();}
    @Override protected void onStop(){prefs.unregisterOnSharedPreferenceChangeListener(this); stopPreview(); super.onStop();}
    @Override public void onSharedPreferenceChanged(SharedPreferences p,String key){render();}
    int dp(float x){return Math.round(x*getResources().getDisplayMetrics().density);}
    void clear(){page.removeAllViews();}
    TextView label(String value,float size,int color,boolean bold,int top){
        TextView t=new TextView(this); t.setText(value); t.setTextSize(size); t.setTextColor(color);
        if(bold)t.setTypeface(Typeface.DEFAULT,Typeface.BOLD);
        t.setPadding(0,dp(top),0,dp(4)); t.setIncludeFontPadding(false);
        page.addView(t); return t;
    }
    Button button(String title,boolean primary,Runnable click){
        Button b=new Button(this); b.setAllCaps(false); b.setText(title); b.setTextSize(15);
        b.setTextColor(primary?BG:INK); b.setGravity(Gravity.CENTER);
        b.setPadding(dp(8),dp(5),dp(8),dp(5)); b.setMinHeight(dp(48));
        GradientDrawable shape=new GradientDrawable(); shape.setColor(primary?ACCENT:PANEL); shape.setCornerRadius(dp(19));
        b.setBackground(new android.graphics.drawable.RippleDrawable(android.content.res.ColorStateList.valueOf(0x44000000),shape,null));
        LinearLayout.LayoutParams lp=new LinearLayout.LayoutParams(-1,-2);lp.topMargin=dp(8);page.addView(b,lp);
        b.setOnClickListener(v->click.run());return b;
    }
    void notice(String msg){Toast.makeText(this,msg,Toast.LENGTH_LONG).show();}
    void stopPreview(){if(player!=null){player.release();player=null;}}
    boolean previewing(){return player!=null;}
    void preview(Uri uri){
        if(previewing()){stopPreview();render();return;}
        try{
            player=new MediaPlayer();player.setAudioAttributes(new AudioAttributes.Builder().setUsage(AudioAttributes.USAGE_MEDIA).build());
            player.setDataSource(this,uri);
            player.setOnPreparedListener(mp->{if(mp==player)mp.start();});
            player.setOnCompletionListener(mp->{stopPreview();render();});
            player.setOnErrorListener((mp,w,e)->{stopPreview();render();notice("파일을 재생할 수 없습니다.");return true;});
            player.prepareAsync();render();
        }catch(Exception ex){stopPreview();notice("파일을 열 수 없습니다.");render();}
    }
}