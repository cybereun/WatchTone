package kr.lebistudio.watchtone;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.Binder;
import android.os.ParcelFileDescriptor;
import android.os.Process;
import android.provider.OpenableColumns;
import android.util.AtomicFile;
import android.util.Log;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;

public final class CallSoundProvider extends ContentProvider {
    private static final String PATH = "selected.mp3";

    static Uri selectedUri(Context context) {
        return new Uri.Builder().scheme("content")
                .authority(context.getPackageName() + ".callsound").appendPath(PATH).build();
    }

    private static AtomicFile selectedFile(Context context) {
        Context storage = context.createDeviceProtectedStorageContext();
        return new AtomicFile(new File(storage.getFilesDir(), PATH));
    }

    static void prepare(Context context, Uri source) throws Exception {
        AtomicFile file = selectedFile(context);
        try (InputStream input = context.getContentResolver().openInputStream(source)) {
            FileOutputStream output = file.startWrite();
            try {
                Transfer.copy(input, output);
                file.finishWrite(output);
            } catch (Exception e) {
                file.failWrite(output);
                throw e;
            }
        }
    }

    private boolean isSelected(Uri uri) {
        return selectedUri(getContext()).getPath().equals(uri.getPath());
    }

    private void checkReader() {
        int uid = Binder.getCallingUid();
        if (uid != Process.SYSTEM_UID && uid != Process.myUid()) {
            throw new SecurityException("Selected ringtone is restricted to the system call service");
        }
    }

    @Override public boolean onCreate() { return true; }

    @Override public String getType(Uri uri) {
        return isSelected(uri) ? "audio/mpeg" : null;
    }

    @Override public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        checkReader();
        if (!isSelected(uri) || !"r".equals(mode)) throw new FileNotFoundException();
        File file = selectedFile(getContext()).getBaseFile();
        ParcelFileDescriptor fd = ParcelFileDescriptor.open(file, ParcelFileDescriptor.MODE_READ_ONLY);
        Log.i("WatchTone", "Selected ringtone opened by uid=" + Binder.getCallingUid());
        return fd;
    }

    @Override public Cursor query(Uri uri, String[] projection, String selection, String[] args, String order) {
        checkReader();
        String[] columns = projection == null
                ? new String[]{OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE} : projection;
        MatrixCursor cursor = new MatrixCursor(columns);
        File file = selectedFile(getContext()).getBaseFile();
        if (isSelected(uri) && file.isFile()) {
            MatrixCursor.RowBuilder row = cursor.newRow();
            row.add(OpenableColumns.DISPLAY_NAME, "워치벨.mp3");
            row.add(OpenableColumns.SIZE, file.length());
        }
        return cursor;
    }

    @Override public Uri insert(Uri uri, ContentValues values) { throw new UnsupportedOperationException(); }
    @Override public int update(Uri uri, ContentValues values, String selection, String[] args) { throw new UnsupportedOperationException(); }
    @Override public int delete(Uri uri, String selection, String[] args) { throw new UnsupportedOperationException(); }
}
