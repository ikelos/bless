project(
    'bless',
    'cs',
    version: '0.7.0',
    meson_version: '>= 0.46',
    default_options: ['libdir=lib'],
)

add_project_arguments('-d:ENABLE_UNIX_SPECIFIC', language: 'cs')

gtk_sharp_2_dep = dependency('gtk-sharp-3.0', version: '>= 3.22.0')
glib_sharp_2_dep = dependency('glib-sharp-3.0', version: '>= 3.22.0')

# Verify the underlying GTK+ C library is >= 3.22 when headers are available.
# On systems where gtk+-3.0 is not on the default PKG_CONFIG_PATH this is a
# non-fatal advisory check; the runtime version is what ultimately matters.
gtk3_c_dep = dependency('gtk+-3.0', version: '>= 3.22', required: false)
if not gtk3_c_dep.found()
    message('gtk+-3.0 C library not found via pkg-config; ' +
            'ensure GTK+ >= 3.22 is installed at runtime')
endif

subdir('src')
subdir('data')
if get_option('tests')
    subdir('tests')
endif
if get_option('user_doc')
    subdir('doc/user')
endif
