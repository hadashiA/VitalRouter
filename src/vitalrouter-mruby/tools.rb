require 'fileutils'

PLATFORMS = {
  'windows-x64' => 'dll',
  'macOS-arm64' => 'dylib',
  'macOS-x64' => 'dylib',
  'ios-arm64' => 'a',
  'ios-x64' => 'a',
  'linux-x64' => 'so',
  'linux-arm64' => 'so',
  'android-x64' => 'so',
  'android-arm64' => 'so',
}

def sh(cmd)
  puts cmd
  `#{cmd}`
end

def copy_to_unity(build_dir)
  build_dir = File.expand_path(build_dir)
  unity_plugins_dir = File.expand_path('../../VitalRouter.Unity/Assets/VitalRouter.MRuby/Runtime/Plugins', __FILE__)

  Dir.foreach(build_dir) do |dir|
    ext = PLATFORMS[dir]
    next if ext.nil?

    src = File.join(build_dir, dir, 'lib', "libmruby.#{ext}")
    dst = File.join(unity_plugins_dir, dir, "VitalRouter.MRuby.Native.#{ext}")
    FileUtils.cp src, dst, verbose: true

    if ext == 'dylib'
      sh %Q{codesign --sign - -force #{dst}}
    end
  end
end

case ARGV[0]
when 'copy_to_unity'
  copy_to_unity(ARGV[1])
else
  puts "No such command #{ARGV[0]}"
end
