MRuby::CrossBuild.new("windows") do |conf|
  conf.toolchain :visualcpp
  conf.gembox '../../../vitalrouter'
end
